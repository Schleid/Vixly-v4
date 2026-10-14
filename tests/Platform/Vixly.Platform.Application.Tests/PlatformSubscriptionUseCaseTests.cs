using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans.CreatePlatformPlan;
using Vixly.Platform.Application.UseCases.Plans.PublishPlatformPlanVersion;
using Vixly.Platform.Application.UseCases.Plans.StartPlatformPlanDraft;
using Vixly.Platform.Application.UseCases.Plans.UpdatePlatformPlanDraft;
using Vixly.Platform.Application.UseCases.Registration;
using Vixly.Platform.Application.UseCases.Registration.ListPublicPlatformPlans;
using Vixly.Platform.Application.UseCases.Registration.SelectRegistrationPlan;
using Vixly.Platform.Application.UseCases.Registration.StartPlatformTenantRegistration;
using Vixly.Platform.Application.UseCases.Registration.VerifyRegistrationEmail;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Application.UseCases.Subscriptions.ChangePlatformSubscription;
using Vixly.Platform.Application.UseCases.Subscriptions.GetPlatformTenantEntitlements;
using Vixly.Platform.Application.UseCases.Subscriptions.GetPlatformTenantSubscription;
using Vixly.Platform.Application.UseCases.Subscriptions.PreviewPlatformSubscriptionChange;
using Vixly.Platform.Application.UseCases.Subscriptions.RecordPlatformOfflinePayment;
using Vixly.Platform.Application.UseCases.Subscriptions.SetPlatformTenantLimitOverride;
using Vixly.Platform.Application.UseCases.Subscriptions.StartPlatformSubscription;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformSubscriptionUseCaseTests.cs
//
// PURPOSE:
// Proves the plan and subscription use cases (Slice 3).
//
// RESPONSIBILITIES:
// - Permissions and step-up are enforced
// - Each change writes history, audit and an outbox message
// - Registration chooses a public plan; the clock and the handlers move subscriptions (D-S2, D-S10, D-S12, D-S13)
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformSubscriptionUseCaseTests
{
    private readonly SubscriptionTestKit _kit = new();

    [Fact]
    public async Task CreatePlan_NeedsThePermission_ThenMakesADraft_AndRefusesATakenCode()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => CreatePlanAsync(_kit.Staff(PlatformPermissions.PlansView), "GROWTH"));

        PlatformAuthorizationContext manager = _kit.Staff(PlatformPermissions.PlansManage);
        CreatePlatformPlanResponse created = await CreatePlanAsync(manager, "growth");

        PlatformPlan plan = Assert.Single(_kit.Store.Plans);
        Assert.True(created.Succeeded);
        Assert.Equal("GROWTH", plan.Code);
        Assert.NotNull(plan.Draft);
        Assert.Equal("platform.plans.created", Assert.Single(_kit.Tenants.Store.AuditEntries).ActionCode);
        Assert.Equal("platform.plans.code_taken", (await CreatePlanAsync(manager, "GROWTH")).Error!.Code);
    }

    [Fact]
    public async Task Publish_NeedsStepUp_AndAnnouncesTheVersion()
    {
        PlatformPlan plan = _kit.Plan();
        plan.StartDraft(_kit.Now);
        plan.EditDraft(PlatformPlanVersionTerms.Create("USD", 12m, 120m, 50m, 2, 30, 10, 2, null), _kit.Now);
        plan.SubmitDraft(Guid.NewGuid(), _kit.Now);
        PlatformAuthorizationContext stale = _kit.Staff(PlatformPermissions.PlansPublish);

        _kit.Tenants.Clock.Advance(TimeSpan.FromMinutes(6));
        Assert.Equal(PlatformCommonErrors.StepUpRequired, (await PublishAsync(stale, plan)).Error);

        PublishPlatformPlanVersionResponse published = await PublishAsync(_kit.Staff(PlatformPermissions.PlansPublish), plan);

        Assert.True(published.Succeeded);
        Assert.Equal(2, plan.Current!.Number);
        PlatformOutboxMessage message = Assert.Single(_kit.Tenants.Tenants.Outbox);
        Assert.Equal(PlatformMessageTypes.PlanVersionPublished, message.Type);
    }

    [Fact]
    public async Task PublicCatalogue_ShowsOnlyActivePublicPublishedPlans()
    {
        _kit.Plan("BASIC");
        _kit.Plan("PRIVATE", isPublic: false);
        _kit.Plan("OLD").Retire(_kit.Now);
        _kit.Store.Plans.Add(PlatformPlan.Create("DRAFTONLY", "Draft only", null, "USD", Guid.NewGuid(), _kit.Now));

        ListPublicPlatformPlansResponse response = await new ListPublicPlatformPlansUseCase(_kit.Store).ExecuteAsync(
            PlatformAnonymousAuthorizationContext.Instance,
            new ListPublicPlatformPlansRequest { Language = "fr" },
            CancellationToken.None);

        PlatformPublicPlanItem item = Assert.Single(response.Plans);
        Assert.Equal("BASIC", item.Code);
        Assert.Equal(30, item.TrialDays);
    }

    [Fact]
    public async Task StaffStart_RefusesASecondSubscription_AndRecordsTheFirst()
    {
        PlatformTenant tenant = _kit.Tenants.Tenant(active: true);
        PlatformPlan plan = _kit.Plan();
        PlatformAuthorizationContext context = _kit.Staff(PlatformPermissions.SubscriptionsManage);

        StartPlatformSubscriptionResponse first = await StartAsync(context, tenant.Id, plan, trial: true);
        StartPlatformSubscriptionResponse second = await StartAsync(context, tenant.Id, plan, trial: false);

        PlatformSubscription subscription = Assert.Single(_kit.Store.Subscriptions);
        Assert.True(first.Succeeded);
        Assert.Equal(PlatformSubscriptionStatus.Trialing, subscription.Status);
        Assert.Equal("platform.subscriptions.already_subscribed", second.Error!.Code);
        Assert.Equal(PlatformSubscriptionEventCodes.StartedTrial, Assert.Single(_kit.Store.Events).EventCode);
        Assert.Equal(PlatformMessageTypes.SubscriptionStarted, Assert.Single(_kit.Tenants.Tenants.Outbox).Type);
    }

    [Fact]
    public async Task PreviewAndChange_Agree_AndTheUpgradeChargeIsPublished()
    {
        PlatformTenant tenant = _kit.Tenants.Tenant(active: true);
        PlatformPlan basic = _kit.Plan("BASIC", 10m);
        PlatformPlan growth = _kit.Plan("GROWTH", 20m);
        _kit.Subscribe(tenant.Id, basic);
        PlatformAuthorizationContext context = _kit.Staff(PlatformPermissions.SubscriptionsManage, PlatformPermissions.SubscriptionsView);

        PreviewPlatformSubscriptionChangeResponse preview = await new PreviewPlatformSubscriptionChangeUseCase(_kit.Subscriptions()).ExecuteAsync(
            context,
            new PreviewPlatformSubscriptionChangeRequest { PlatformTenantId = tenant.Id, PlatformPlanId = growth.Id, Term = PlatformSubscriptionTerm.Monthly, Licences = 3 },
            CancellationToken.None);
        ChangePlatformSubscriptionResponse change = await new ChangePlatformSubscriptionUseCase(_kit.Subscriptions(), _kit.Tenants.Store).ExecuteAsync(
            context,
            new ChangePlatformSubscriptionRequest { PlatformTenantId = tenant.Id, PlatformPlanId = growth.Id, Term = PlatformSubscriptionTerm.Monthly, Licences = 3 },
            CancellationToken.None);

        Assert.Equal(preview.Quote, change.Quote);
        Assert.Equal(PlatformSubscriptionChangeTiming.Now, change.Quote!.Timing);
        Assert.True(change.Quote.ChargeNow > 0m);
        PlatformSubscriptionMessage message = PlatformOutboxWriter.Read<PlatformSubscriptionMessage>(_kit.Tenants.Tenants.Outbox.Single(item => item.Type == PlatformMessageTypes.SubscriptionChanged));
        Assert.Equal(change.Quote.ChargeNow, message.ChargeNow);
        Assert.Equal(growth.Id, message.PlatformPlanId);
    }

    [Fact]
    public async Task Override_NeedsStepUp_ReplacesTheOpenOne_AndRaisesTheLicenceCeiling()
    {
        PlatformTenant tenant = _kit.Tenants.Tenant(active: true);
        PlatformPlan plan = _kit.Plan(maxDevices: 3);
        PlatformAuthorizationContext context = _kit.Staff(PlatformPermissions.SubscriptionsOverrideLimits, PlatformPermissions.SubscriptionsManage, PlatformPermissions.SubscriptionsView);

        await OverrideAsync(context, tenant.Id, 5);
        await OverrideAsync(context, tenant.Id, 8);

        Assert.Single(_kit.Store.Overrides, item => item.IsOpen);
        Assert.Equal(8, _kit.Store.Overrides.Single(item => item.IsOpen).Value);
        Assert.True((await StartAsync(context, tenant.Id, plan, false, licences: 8)).Succeeded);

        GetPlatformTenantEntitlementsResponse entitlements = await new GetPlatformTenantEntitlementsUseCase(_kit.Subscriptions()).ExecuteAsync(
            context, new GetPlatformTenantEntitlementsRequest { PlatformTenantId = tenant.Id }, CancellationToken.None);

        Assert.Equal(8, entitlements.Entitlements!.Limits[PlatformPlanLimitKind.MaxLicensedDevices]);
        Assert.Equal(PlatformSubscriptionAccess.ReadOnly, entitlements.Entitlements.Access);
        Assert.Contains(PlatformPlanFeature.DataExport, entitlements.Entitlements.Features);
    }

    [Fact]
    public async Task OfflinePayment_NeedsStepUp_AndActivates()
    {
        PlatformTenant tenant = _kit.Tenants.Tenant(active: true);
        PlatformSubscription subscription = _kit.Subscribe(tenant.Id, _kit.Plan(), paid: false);
        PlatformAuthorizationContext context = _kit.Staff(PlatformPermissions.SubscriptionsManage);

        RecordPlatformOfflinePaymentResponse paid = await new RecordPlatformOfflinePaymentUseCase(_kit.Subscriptions(), _kit.Tenants.Store).ExecuteAsync(
            context, new RecordPlatformOfflinePaymentRequest { PlatformTenantId = tenant.Id, Reason = "Wire 1042" }, CancellationToken.None);

        Assert.True(paid.Succeeded);
        Assert.Equal(PlatformSubscriptionStatus.Active, subscription.Status);
        PlatformSubscriptionMessage message = PlatformOutboxWriter.Read<PlatformSubscriptionMessage>(Assert.Single(_kit.Tenants.Tenants.Outbox));
        Assert.Equal(50m, message.SetupFee);
        Assert.Equal(30m, message.PeriodAmount);
    }

    [Fact]
    public async Task Registration_ChoosesAPublicPlan_WithATrial_Once()
    {
        _kit.Plan("BASIC");
        _kit.Plan("PRIVATE", isPublic: false);
        string access = await VerifiedRegistrationAsync();

        SelectRegistrationPlanResponse hidden = await SelectAsync(access, "PRIVATE", trial: false);
        SelectRegistrationPlanResponse chosen = await SelectAsync(access, "basic", trial: true);
        SelectRegistrationPlanResponse again = await SelectAsync(access, "BASIC", trial: false);

        Assert.Equal("platform.subscriptions.plan_not_available", hidden.Error!.Code);
        Assert.Equal(PlatformTenantRegistrationStatus.PendingAccountSetup, chosen.Step);
        Assert.NotNull(chosen.TrialEndsUtc);
        Assert.Equal(PlatformSubscriptionStatus.Trialing, Assert.Single(_kit.Store.Subscriptions).Status);
        Assert.Equal("platform.registrations.plan_already_chosen", again.Error!.Code);
    }

    [Fact]
    public async Task Clock_ExpiresTrials_AndRenewsOnTheCurrentVersion_Once()
    {
        PlatformTenant first = _kit.Tenants.Tenant("first", active: true);
        PlatformTenant second = _kit.Tenants.Tenant("second", active: true);
        PlatformPlan plan = _kit.Plan();
        PlatformSubscription paid = _kit.Subscribe(first.Id, plan);
        PlatformSubscription trial = PlatformSubscription.Start(second.Id, plan, PlatformSubscriptionTerm.Monthly, 3, 10, true, null, _kit.Now.AddDays(-31)).Subscription;
        _kit.Store.Subscriptions.Add(trial);
        plan.StartDraft(_kit.Now);
        plan.EditDraft(PlatformPlanVersionTerms.Create("USD", 12m, 120m, 50m, 2, 30, 10, 2, null), _kit.Now);
        plan.PublishDraft(Guid.NewGuid(), _kit.Now);

        _kit.Tenants.Clock.Advance(TimeSpan.FromDays(20));
        PlatformSubscriptionClock clock = new(_kit.Subscriptions(), _kit.Tenants.Store, _kit.Sales, _kit.Tenants.Outbox());

        Assert.Equal(2, await clock.ProcessDueAsync(CancellationToken.None));
        Assert.Equal(0, await clock.ProcessDueAsync(CancellationToken.None));
        Assert.Equal(PlatformSubscriptionStatus.PendingPayment, trial.Status);
        Assert.Equal(plan.Current!.Id, paid.PlanVersionId);
        PlatformSubscriptionMessage renewed = PlatformOutboxWriter.Read<PlatformSubscriptionMessage>(_kit.Tenants.Tenants.Outbox.Single(item => item.Type == PlatformMessageTypes.SubscriptionRenewed));
        Assert.Equal(36m, renewed.PeriodAmount);
    }

    [Theory]
    [InlineData(PlatformTenantStatus.Suspended, PlatformSubscriptionStatus.Suspended)]
    [InlineData(PlatformTenantStatus.Banned, PlatformSubscriptionStatus.Ended)]
    [InlineData(PlatformTenantStatus.Deactivated, PlatformSubscriptionStatus.Ended)]
    public async Task TenantStatus_MovesTheSubscription(PlatformTenantStatus tenantStatus, PlatformSubscriptionStatus expected)
    {
        PlatformTenant tenant = _kit.Tenants.Tenant(active: true);
        PlatformSubscription subscription = _kit.Subscribe(tenant.Id, _kit.Plan());

        await new PlatformSubscriptionTenantStatusHandler(_kit.Subscriptions()).HandleAsync(
            StatusMessage(tenant, PlatformTenantStatus.Active, tenantStatus),
            CancellationToken.None);

        Assert.Equal(expected, subscription.Status);

        if (tenantStatus == PlatformTenantStatus.Suspended)
        {
            await new PlatformSubscriptionTenantStatusHandler(_kit.Subscriptions()).HandleAsync(
                StatusMessage(tenant, PlatformTenantStatus.Suspended, PlatformTenantStatus.Active),
                CancellationToken.None);
            Assert.Equal(PlatformSubscriptionStatus.Active, subscription.Status);
        }
    }

    [Fact]
    public async Task PriceIncrease_IsAnnouncedToTheBillingContact_DecreasesAreNot()
    {
        PlatformTenant tenant = _kit.Tenants.Tenant(active: true);
        PlatformPlan plan = _kit.Plan();
        PlatformSubscription subscription = _kit.Subscribe(tenant.Id, plan);
        Guid previous = plan.Current!.Id;

        plan.StartDraft(_kit.Now);
        plan.EditDraft(PlatformPlanVersionTerms.Create("USD", 12m, 120m, 50m, 2, 30, 10, 2, null), _kit.Now);
        PlatformPlanVersion next = plan.PublishDraft(Guid.NewGuid(), _kit.Now);
        PlatformPriceIncreaseNoticeHandler handler = new(_kit.Store, _kit.Store, _kit.Tenants.Tenants, _kit.Store);

        await handler.HandleAsync(Published(plan, next, previous), CancellationToken.None);

        var notice = Assert.Single(_kit.Store.PriceNotices);
        Assert.Equal(("owner@northwind.example", 30m, 36m, subscription.PeriodEndUtc!.Value), (notice.To, notice.Before, notice.After, notice.Effective));

        plan.StartDraft(_kit.Now);
        plan.EditDraft(PlatformPlanVersionTerms.Create("USD", 8m, 80m, 50m, 2, 30, 10, 2, null), _kit.Now);
        PlatformPlanVersion cheaper = plan.PublishDraft(Guid.NewGuid(), _kit.Now);
        await handler.HandleAsync(Published(plan, cheaper, next.Id), CancellationToken.None);

        Assert.Single(_kit.Store.PriceNotices);
    }

    private Task<CreatePlatformPlanResponse> CreatePlanAsync(PlatformAuthorizationContext context, string code)
    {
        return new CreatePlatformPlanUseCase(_kit.Plans(), _kit.Store, _kit.Tenants.Store).ExecuteAsync(
            context,
            new CreatePlatformPlanRequest { Code = code, Name = "Growth", CurrencyCode = "USD" },
            CancellationToken.None);
    }

    private Task<PublishPlatformPlanVersionResponse> PublishAsync(PlatformAuthorizationContext context, PlatformPlan plan)
    {
        return new PublishPlatformPlanVersionUseCase(_kit.Plans(), _kit.Tenants.Outbox(), _kit.Tenants.Store).ExecuteAsync(
            context,
            new PublishPlatformPlanVersionRequest { PlatformPlanId = plan.Id },
            CancellationToken.None);
    }

    [Theory]
    [InlineData(PlatformTenantStatus.Draft, true)]
    [InlineData(PlatformTenantStatus.Active, true)]
    [InlineData(PlatformTenantStatus.Suspended, false)]
    [InlineData(PlatformTenantStatus.Deactivated, false)]
    [InlineData(PlatformTenantStatus.Banned, false)]
    public async Task Start_IsOfferedAndAllowed_OnlyWhileTheTenantIsOpen(PlatformTenantStatus status, bool allowed)
    {
        PlatformTenant tenant = TenantIn(status);
        PlatformAuthorizationContext context = _kit.Staff(PlatformPermissions.SubscriptionsView, PlatformPermissions.SubscriptionsManage);
        _kit.Plan();

        GetPlatformTenantSubscriptionResponse view = await new GetPlatformTenantSubscriptionUseCase(_kit.Subscriptions(), _kit.Store, _kit.Tenants.Tenants).ExecuteAsync(
            context,
            new GetPlatformTenantSubscriptionRequest { PlatformTenantId = tenant.Id, Language = "en" },
            CancellationToken.None);
        StartPlatformSubscriptionResponse start = await StartAsync(context, tenant.Id, _kit.Store.Plans.Single(), trial: false);

        Assert.True(view.Succeeded);
        Assert.Equal(allowed, view.Abilities.CanStart);
        Assert.Equal(allowed, start.Succeeded);

        if (!allowed)
        {
            Assert.Equal("platform.subscriptions.tenant_cannot_subscribe", start.Error!.Code);
            Assert.Empty(_kit.Store.Subscriptions);
        }
    }

    private PlatformTenant TenantIn(PlatformTenantStatus status)
    {
        PlatformTenant tenant = _kit.Tenants.Tenant(active: status != PlatformTenantStatus.Draft);
        Guid actor = Guid.NewGuid();

        switch (status)
        {
            case PlatformTenantStatus.Suspended:
                tenant.Suspend(PlatformSuspensionCategory.Compliance, "Licence expired", actor, _kit.Now);
                break;
            case PlatformTenantStatus.Deactivated:
                tenant.Deactivate("Closed", actor, _kit.Now);
                break;
            case PlatformTenantStatus.Banned:
                tenant.Ban("Fraud", actor, _kit.Now);
                break;
        }

        return tenant;
    }

    private Task<StartPlatformSubscriptionResponse> StartAsync(PlatformAuthorizationContext context, Guid tenantId, PlatformPlan plan, bool trial, int licences = 3)
    {
        return new StartPlatformSubscriptionUseCase(_kit.Subscriptions(), _kit.Tenants.Tenants, _kit.Tenants.Store).ExecuteAsync(
            context,
            new StartPlatformSubscriptionRequest { PlatformTenantId = tenantId, PlatformPlanId = plan.Id, Term = PlatformSubscriptionTerm.Monthly, Licences = licences, StartTrial = trial },
            CancellationToken.None);
    }

    private Task<SetPlatformTenantLimitOverrideResponse> OverrideAsync(PlatformAuthorizationContext context, Guid tenantId, int value)
    {
        return new SetPlatformTenantLimitOverrideUseCase(_kit.Subscriptions(), _kit.Tenants.Tenants, _kit.Tenants.Store).ExecuteAsync(
            context,
            new SetPlatformTenantLimitOverrideRequest { PlatformTenantId = tenantId, Kind = PlatformPlanLimitKind.MaxLicensedDevices, Value = value, Reason = "Pilot" },
            CancellationToken.None);
    }

    private Task<SelectRegistrationPlanResponse> SelectAsync(string access, string code, bool trial)
    {
        return new SelectRegistrationPlanUseCase(_kit.Tenants.Links(), _kit.Subscriptions(), _kit.SalesAdministration(), _kit.Tenants.Audit(), _kit.Tenants.Store).ExecuteAsync(
            PlatformAnonymousAuthorizationContext.Instance,
            new SelectRegistrationPlanRequest { AccessToken = access, PlanCode = code, Term = PlatformSubscriptionTerm.Monthly, Licences = 3, StartTrial = trial },
            CancellationToken.None);
    }

    private async Task<string> VerifiedRegistrationAsync()
    {
        TenantTestKit kit = _kit.Tenants;
        await new StartPlatformTenantRegistrationUseCase(
            kit.Tenants, kit.Store, kit.Tenants, kit.Slugs(), kit.Tenants, kit.Tenants, kit.Links(), kit.Crypto, kit.Tenants, kit.Audit(), kit.Store, kit.Clock).ExecuteAsync(
                PlatformAnonymousAuthorizationContext.Instance,
                new StartPlatformTenantRegistrationRequest
                {
                    Company = TenantTestKit.Input(),
                    OwnerName = "Grace Hopper",
                    OwnerEmail = "grace@northwind.example",
                    OwnerPhone = "+1 514 555 0101",
                    Language = "en",
                    TimeZone = "America/Toronto",
                    Slug = "Northwind",
                    AcceptedTerms = true,
                    TermsVersion = "2026-10",
                    PrivacyVersion = "2026-09",
                    BotToken = "human"
                },
                CancellationToken.None);
        VerifyRegistrationEmailResponse verified = await new VerifyRegistrationEmailUseCase(
            kit.Tenants, kit.Tenants, kit.Tenants, kit.Administration(), kit.Crypto, kit.Audit(), kit.Store, kit.Clock).ExecuteAsync(
                PlatformAnonymousAuthorizationContext.Instance,
                new VerifyRegistrationEmailRequest { Token = kit.Tenants.Mail[0].Value },
                CancellationToken.None);

        return verified.AccessToken;
    }

    private static PlatformOutboxMessage StatusMessage(PlatformTenant tenant, PlatformTenantStatus from, PlatformTenantStatus to)
    {
        return PlatformOutboxMessage.Create(
            PlatformMessageTypes.TenantStatusChanged,
            tenant.Id,
            System.Text.Json.JsonSerializer.Serialize(
                new PlatformTenantStatusChanged(tenant.Id, tenant.TenantCode, from, to, null, TenantTestKit.Now),
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)),
            TenantTestKit.Now);
    }

    private static PlatformOutboxMessage Published(PlatformPlan plan, PlatformPlanVersion version, Guid previous)
    {
        return PlatformOutboxMessage.Create(
            PlatformMessageTypes.PlanVersionPublished,
            plan.Id,
            System.Text.Json.JsonSerializer.Serialize(
                new PlatformPlanVersionPublished(plan.Id, plan.Code, version.Id, version.Number, previous, TenantTestKit.Now),
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)),
            TenantTestKit.Now);
    }
}
