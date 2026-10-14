using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans.PublishPlatformPlanVersion;
using Vixly.Platform.Application.UseCases.Plans.SubmitPlatformPlanVersion;
using Vixly.Platform.Application.UseCases.Registration.GetRegistrationOffer;
using Vixly.Platform.Application.UseCases.Registration.SelectRegistrationPlan;
using Vixly.Platform.Application.UseCases.Registration.StartPlatformTenantRegistration;
using Vixly.Platform.Application.UseCases.Registration.VerifyRegistrationEmail;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Application.UseCases.Sales.ListPlatformApprovals;
using Vixly.Platform.Application.UseCases.Sales.PreparePlatformOffer;
using Vixly.Platform.Application.UseCases.Sales.SubmitPlatformOffer;
using Vixly.Platform.Application.UseCases.Sales.UpdatePlatformSalesSettings;
using Vixly.Platform.Application.UseCases.Subscriptions.ApplyPlatformPromotion;
using Vixly.Platform.Application.UseCases.Subscriptions.ApprovePlatformDiscount;
using Vixly.Platform.Application.UseCases.Subscriptions.GrantPlatformDiscount;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformSalesUseCaseTests.cs
//
// PURPOSE:
// Proves the Slice 3b use cases: discounts with approval, plan approval, offer links, promotion codes and notices (D-S18 to D-S26).
//
// RESPONSIBILITIES:
// - Nothing preset: empty Sales settings send every discount to approval
// - Four eyes on discounts, offers and plan versions
// - Offer links and promotion codes at registration
// - The discount-ending notice runs only when its days are set
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformSalesUseCaseTests
{
    private readonly SubscriptionTestKit _kit = new();

    private PlatformDiscountTermsInput Percent(decimal value, int periods = 3)
    {
        return new PlatformDiscountTermsInput(PlatformDiscountKind.Percent, value, PlatformDiscountDuration.Periods, periods, null);
    }

    private void Limits(decimal? percent = null, int? offerDays = null, int? noticeDays = null, bool codes = false)
    {
        PlatformSalesSettings settings = PlatformSalesSettings.Empty(_kit.Now);
        settings.Update(percent, new Dictionary<string, decimal>(), false, offerDays, noticeDays, codes, Guid.NewGuid(), _kit.Now);
        _kit.Sales.Settings = settings;
    }

    [Fact]
    public async Task Grant_WithNothingSet_WaitsForTheSalesManager_WhoIsNotTheRequester()
    {
        PlatformTenant tenant = _kit.Tenants.Tenant(active: true);
        PlatformSubscription subscription = _kit.Subscribe(tenant.Id, _kit.Plan());
        PlatformAuthorizationContext sales = _kit.Staff(PlatformPermissions.DiscountsGrant, PlatformPermissions.DiscountsApprove);

        GrantPlatformDiscountResponse granted = await GrantAsync(sales, tenant.Id, Percent(5m));

        Assert.True(granted.NeedsApproval);
        Assert.Null(subscription.ActiveDiscount);
        Assert.Contains(_kit.Tenants.Tenants.Outbox, message => message.Type == PlatformMessageTypes.ApprovalRequested);

        Guid pending = subscription.PendingDiscount!.Id;
        ApprovePlatformDiscountResponse own = await ApproveAsync(sales, tenant.Id, pending);
        ApprovePlatformDiscountResponse other = await ApproveAsync(_kit.Staff(PlatformPermissions.DiscountsApprove), tenant.Id, pending);

        Assert.Equal("platform.discounts.own_request", own.Error!.Code);
        Assert.True(other.Succeeded);
        Assert.NotNull(subscription.ActiveDiscount);
    }

    [Fact]
    public async Task Grant_WithinTheLimit_AppliesAtOnce_AndBeyondIt_Waits()
    {
        Limits(percent: 20m);
        PlatformTenant tenant = _kit.Tenants.Tenant(active: true);
        PlatformSubscription subscription = _kit.Subscribe(tenant.Id, _kit.Plan());
        PlatformAuthorizationContext sales = _kit.Staff(PlatformPermissions.DiscountsGrant);

        Assert.False((await GrantAsync(sales, tenant.Id, Percent(20m))).NeedsApproval);
        Assert.Equal(20m, subscription.ActiveDiscount!.Terms.Value);
        Assert.True((await GrantAsync(sales, tenant.Id, Percent(25m))).NeedsApproval);
        Assert.Equal(20m, subscription.ActiveDiscount!.Terms.Value);
    }

    [Fact]
    public async Task PlanVersion_SubmittedBySales_IsPublishedOnlyByAnotherPerson()
    {
        PlatformPlan plan = _kit.Plan();
        plan.StartDraft(_kit.Now);
        PlatformAuthorizationContext both = _kit.Staff(PlatformPermissions.PlansManage, PlatformPermissions.PlansPublish);

        SubmitPlatformPlanVersionResponse submitted = await new SubmitPlatformPlanVersionUseCase(_kit.Plans(), _kit.SalesAdministration(), _kit.Tenants.Store).ExecuteAsync(
            both, new SubmitPlatformPlanVersionRequest { PlatformPlanId = plan.Id }, CancellationToken.None);
        PublishPlatformPlanVersionResponse own = await PublishAsync(both, plan);
        PublishPlatformPlanVersionResponse cfo = await PublishAsync(_kit.Staff(PlatformPermissions.PlansPublish), plan);

        Assert.True(submitted.Succeeded);
        Assert.Equal("platform.plans.own_submission", own.Error!.Code);
        Assert.True(cfo.Succeeded);
        Assert.Equal(2, plan.Current!.Number);
    }

    [Fact]
    public async Task OfferLink_WithinTheLimits_IsIssued_AndRegistrationGetsTheAgreedTerms()
    {
        Limits(percent: 100m, offerDays: 30);
        PlatformPlan plan = _kit.Plan(isPublic: false);
        PlatformAuthorizationContext sales = _kit.Staff(PlatformPermissions.OffersManage);
        Guid offerId = (await PrepareAsync(sales, plan, email: "grace@northwind.example")).OfferId;

        SubmitPlatformOfferResponse issued = await new SubmitPlatformOfferUseCase(_kit.SalesAdministration(), _kit.Store, _kit.Tenants.Store).ExecuteAsync(
            sales, new SubmitPlatformOfferRequest { OfferId = offerId }, CancellationToken.None);
        string token = issued.Link!.Split("offer=")[1];

        GetRegistrationOfferResponse preview = await new GetRegistrationOfferUseCase(_kit.SalesAdministration(), _kit.Store).ExecuteAsync(
            PlatformAnonymousAuthorizationContext.Instance, new GetRegistrationOfferRequest { Token = token }, CancellationToken.None);
        SelectRegistrationPlanResponse chosen = await SelectAsync(await VerifiedRegistrationAsync(), token);

        Assert.False(issued.NeedsApproval);
        Assert.Equal("g****@northwind.example", preview.Offer!.MaskedEmail);
        Assert.True(chosen.Succeeded);
        PlatformSubscription subscription = Assert.Single(_kit.Store.Subscriptions);
        Assert.Equal(PlatformDiscountSource.Offer, subscription.ActiveDiscount!.Source);
        Assert.Equal(0m, subscription.SetupFeeOverride);
        Assert.Equal("Data migration", Assert.Single(subscription.Charges).Description);
        Assert.Equal(PlatformOfferStatus.Used, _kit.Sales.Offers.Single().Status);
        Assert.Equal("platform.offers.unavailable", (await new GetRegistrationOfferUseCase(_kit.SalesAdministration(), _kit.Store).ExecuteAsync(
            PlatformAnonymousAuthorizationContext.Instance, new GetRegistrationOfferRequest { Token = token }, CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task OfferLink_TiedToAnotherEmail_IsRefused()
    {
        Limits(percent: 100m, offerDays: 30);
        PlatformPlan plan = _kit.Plan();
        PlatformAuthorizationContext sales = _kit.Staff(PlatformPermissions.OffersManage);
        Guid offerId = (await PrepareAsync(sales, plan, email: "someone@else.example")).OfferId;
        string token = (await new SubmitPlatformOfferUseCase(_kit.SalesAdministration(), _kit.Store, _kit.Tenants.Store).ExecuteAsync(
            sales, new SubmitPlatformOfferRequest { OfferId = offerId }, CancellationToken.None)).Link!.Split("offer=")[1];

        SelectRegistrationPlanResponse chosen = await SelectAsync(await VerifiedRegistrationAsync(), token);

        Assert.Equal("platform.offers.email_mismatch", chosen.Error!.Code);
        Assert.Empty(_kit.Store.Subscriptions);
    }

    [Fact]
    public async Task OfferLink_WithoutLimits_WaitsForApproval()
    {
        PlatformAuthorizationContext sales = _kit.Staff(PlatformPermissions.OffersManage);
        Guid offerId = (await PrepareAsync(sales, _kit.Plan(), email: null)).OfferId;

        SubmitPlatformOfferResponse submitted = await new SubmitPlatformOfferUseCase(_kit.SalesAdministration(), _kit.Store, _kit.Tenants.Store).ExecuteAsync(
            sales, new SubmitPlatformOfferRequest { OfferId = offerId }, CancellationToken.None);

        Assert.True(submitted.NeedsApproval);
        Assert.Null(submitted.Link);
        Assert.Equal(PlatformOfferStatus.PendingApproval, _kit.Sales.Offers.Single().Status);
    }

    [Fact]
    public async Task PromotionCode_AtRegistration_OnlyWhenTheSettingAllowsIt()
    {
        PlatformPlan plan = _kit.Plan();
        PlatformPromotion promotion = ActivePromotion(plan);

        SelectRegistrationPlanResponse refused = await SelectAsync(await VerifiedRegistrationAsync(), offerToken: null, promotion: promotion.Code, plan: plan.Code);
        Limits(codes: true);
        SelectRegistrationPlanResponse accepted = await SelectAsync(_lastAccess!, offerToken: null, promotion: promotion.Code, plan: plan.Code);

        Assert.Equal("platform.promotions.not_at_registration", refused.Error!.Code);
        Assert.True(accepted.Succeeded);
        Assert.Equal(PlatformDiscountSource.Promotion, _kit.Store.Subscriptions.Single().ActiveDiscount!.Source);
        Assert.Equal(1, promotion.Redemptions);
    }

    [Fact]
    public async Task Promotion_AppliedByStaff_OncePerTenant()
    {
        PlatformTenant tenant = _kit.Tenants.Tenant(active: true);
        PlatformPlan plan = _kit.Plan();
        PlatformSubscription subscription = _kit.Subscribe(tenant.Id, plan);
        PlatformPromotion promotion = ActivePromotion(plan);
        PlatformAuthorizationContext sales = _kit.Staff(PlatformPermissions.DiscountsGrant);

        ApplyPlatformPromotionResponse first = await ApplyAsync(sales, tenant.Id, promotion.Code.ToLowerInvariant());
        subscription.EndDiscount("Test", Guid.NewGuid(), _kit.Now);
        ApplyPlatformPromotionResponse second = await ApplyAsync(sales, tenant.Id, promotion.Code);

        Assert.True(first.Succeeded);
        Assert.Equal("platform.promotions.already_used", second.Error!.Code);
    }

    [Fact]
    public async Task Approvals_ShowOnlyWhatThePersonMayDecide_AndNeverTheirOwn()
    {
        PlatformAuthorizationContext manager = _kit.Staff(PlatformPermissions.DiscountsApprove);
        _kit.Store.Approvals.Add(new PlatformApprovalItem(PlatformApprovalKind.Discount, Guid.NewGuid(), Guid.NewGuid(), "BASIC", null, Guid.NewGuid(), "Sam", _kit.Now, "Loyal"));
        _kit.Store.Approvals.Add(new PlatformApprovalItem(PlatformApprovalKind.Discount, Guid.NewGuid(), Guid.NewGuid(), "BASIC", null, manager.PlatformUserId, "Me", _kit.Now, "Mine"));
        _kit.Store.Approvals.Add(new PlatformApprovalItem(PlatformApprovalKind.PlanVersion, Guid.NewGuid(), null, "PRO v2", null, Guid.NewGuid(), "Sam", _kit.Now, null));

        ListPlatformApprovalsResponse list = await new ListPlatformApprovalsUseCase(_kit.SalesAdministration(), _kit.Store).ExecuteAsync(
            manager, new ListPlatformApprovalsRequest(), CancellationToken.None);

        PlatformApprovalItem item = Assert.Single(list.Items);
        Assert.Equal("Sam", item.RequestedByName);
    }

    [Fact]
    public async Task SalesSettings_NeedStepUp_AndAreAudited()
    {
        PlatformAuthorizationContext stale = _kit.Staff(PlatformPermissions.SalesConfigure);
        _kit.Tenants.Clock.Advance(TimeSpan.FromMinutes(6));

        UpdatePlatformSalesSettingsResponse refused = await UpdateSettingsAsync(stale);
        UpdatePlatformSalesSettingsResponse saved = await UpdateSettingsAsync(_kit.Staff(PlatformPermissions.SalesConfigure));

        Assert.Equal(PlatformCommonErrors.StepUpRequired, refused.Error);
        Assert.True(saved.Succeeded);
        Assert.Equal(15m, _kit.Sales.Settings!.MaxPercentWithoutApproval);
    }

    [Fact]
    public async Task Clock_AnnouncesAnEndingDiscount_OnlyOnceItsDaysAreSet()
    {
        PlatformTenant tenant = _kit.Tenants.Tenant(active: true);
        PlatformSubscription subscription = _kit.Subscribe(tenant.Id, _kit.Plan());
        subscription.RequestDiscount(
            PlatformDiscountTerms.Create(PlatformDiscountKind.Percent, 10m, PlatformDiscountDuration.UntilDate, null, _kit.Now.AddDays(10), _kit.Now),
            PlatformDiscountSource.Staff,
            null,
            "Pilot",
            false,
            Guid.NewGuid(),
            _kit.Now);
        PlatformSubscriptionClock clock = new(_kit.Subscriptions(), _kit.Tenants.Store, _kit.Sales, _kit.Tenants.Outbox());

        await clock.ProcessDueAsync(CancellationToken.None);
        Assert.DoesNotContain(_kit.Tenants.Tenants.Outbox, message => message.Type == PlatformMessageTypes.DiscountEnding);

        Limits(noticeDays: 14);
        await clock.ProcessDueAsync(CancellationToken.None);
        await clock.ProcessDueAsync(CancellationToken.None);

        Assert.Single(_kit.Tenants.Tenants.Outbox, message => message.Type == PlatformMessageTypes.DiscountEnding);
    }

    private PlatformPromotion ActivePromotion(PlatformPlan plan)
    {
        PlatformPromotion promotion = PlatformPromotion.Create(
            "LAUNCH",
            "Launch",
            PlatformDiscountTerms.Create(PlatformDiscountKind.Percent, 100m, PlatformDiscountDuration.Periods, 1, null, _kit.Now),
            _kit.Now.AddDays(-1),
            null,
            new[] { plan.Id },
            null,
            true,
            true,
            null,
            Guid.NewGuid(),
            _kit.Now);
        promotion.Submit(Guid.NewGuid(), _kit.Now);
        promotion.Approve(Guid.NewGuid(), null, _kit.Now);
        _kit.Sales.Promotions.Add(promotion);

        return promotion;
    }

    private Task<GrantPlatformDiscountResponse> GrantAsync(PlatformAuthorizationContext context, Guid tenantId, PlatformDiscountTermsInput terms)
    {
        return new GrantPlatformDiscountUseCase(_kit.Subscriptions(), _kit.SalesAdministration(), _kit.Tenants.Store).ExecuteAsync(
            context, new GrantPlatformDiscountRequest { PlatformTenantId = tenantId, Terms = terms, Reason = "Valued customer" }, CancellationToken.None);
    }

    private Task<ApprovePlatformDiscountResponse> ApproveAsync(PlatformAuthorizationContext context, Guid tenantId, Guid discountId)
    {
        return new ApprovePlatformDiscountUseCase(_kit.Subscriptions(), _kit.Tenants.Store).ExecuteAsync(
            context, new ApprovePlatformDiscountRequest { PlatformTenantId = tenantId, DiscountId = discountId }, CancellationToken.None);
    }

    private Task<ApplyPlatformPromotionResponse> ApplyAsync(PlatformAuthorizationContext context, Guid tenantId, string code)
    {
        return new ApplyPlatformPromotionUseCase(_kit.Subscriptions(), _kit.SalesAdministration(), _kit.Tenants.Store).ExecuteAsync(
            context, new ApplyPlatformPromotionRequest { PlatformTenantId = tenantId, Code = code }, CancellationToken.None);
    }

    private Task<PublishPlatformPlanVersionResponse> PublishAsync(PlatformAuthorizationContext context, PlatformPlan plan)
    {
        return new PublishPlatformPlanVersionUseCase(_kit.Plans(), _kit.Tenants.Outbox(), _kit.Tenants.Store).ExecuteAsync(
            context, new PublishPlatformPlanVersionRequest { PlatformPlanId = plan.Id }, CancellationToken.None);
    }

    private Task<UpdatePlatformSalesSettingsResponse> UpdateSettingsAsync(PlatformAuthorizationContext context)
    {
        return new UpdatePlatformSalesSettingsUseCase(_kit.SalesAdministration(), _kit.Tenants.Store).ExecuteAsync(
            context, new UpdatePlatformSalesSettingsRequest { MaxPercentWithoutApproval = 15m, MaxOfferValidityDays = 14 }, CancellationToken.None);
    }

    private Task<PreparePlatformOfferResponse> PrepareAsync(PlatformAuthorizationContext context, PlatformPlan plan, string? email)
    {
        return new PreparePlatformOfferUseCase(_kit.SalesAdministration(), _kit.Store, _kit.Tenants.Store).ExecuteAsync(
            context,
            new PreparePlatformOfferRequest
            {
                CompanyName = "Northwind",
                BoundEmail = email,
                Reason = "Three-year partnership",
                PlatformPlanId = plan.Id,
                Term = PlatformSubscriptionTerm.Monthly,
                Licences = 5,
                Discount = Percent(100m, periods: 1),
                SetupFeeOverride = 0m,
                Charges = new[] { new PlatformOfferChargeItem("Data migration", 1200m) },
                ExpiresUtc = _kit.Now.AddDays(20),
                UsesAllowed = 1
            },
            CancellationToken.None);
    }

    private string? _lastAccess;

    private Task<SelectRegistrationPlanResponse> SelectAsync(string access, string? offerToken, string? promotion = null, string plan = "")
    {
        _lastAccess = access;

        return new SelectRegistrationPlanUseCase(_kit.Tenants.Links(), _kit.Subscriptions(), _kit.SalesAdministration(), _kit.Tenants.Audit(), _kit.Tenants.Store).ExecuteAsync(
            PlatformAnonymousAuthorizationContext.Instance,
            new SelectRegistrationPlanRequest { AccessToken = access, PlanCode = plan, Term = PlatformSubscriptionTerm.Monthly, Licences = 3, OfferToken = offerToken, PromotionCode = promotion },
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
}
