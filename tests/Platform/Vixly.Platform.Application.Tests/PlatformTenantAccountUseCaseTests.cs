using System.Text.Json;
using Vixly.Contracts.Platform.Events;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Events.ReadPlatformEvents;
using Vixly.Platform.Application.UseCases.Licensing.ApprovePlatformDevice;
using Vixly.Platform.Application.UseCases.Licensing.EnrolPlatformDevice;
using Vixly.Platform.Application.UseCases.Licensing.GetPlatformPortalAccess;
using Vixly.Platform.Application.UseCases.Licensing.RequestPlatformFirstDeviceLink;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Application.UseCases.TenantAccount;
using Vixly.Platform.Application.UseCases.TenantAccount.ChangeTenantSubscription;
using Vixly.Platform.Application.UseCases.TenantAccount.CreateTenantPayLink;
using Vixly.Platform.Application.UseCases.TenantAccount.GetPlatformEntitlementsBatch;
using Vixly.Platform.Application.UseCases.TenantAccount.GetTenantAccount;
using Vixly.Platform.Application.UseCases.TenantAccount.GetTenantBillingDocument;
using Vixly.Platform.Application.UseCases.TenantAccount.ListTenantPlanOptions;
using Vixly.Platform.Application.UseCases.TenantAccount.UpdateTenantAccount;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformTenantAccountUseCaseTests.cs
//
// PURPOSE:
// Proves Slice 9 over fakes: tenant roles and step-up, tenant isolation, the tenant's own account and subscription calls, and the public event feed.
//
// RESPONSIBILITIES:
// - Role and step-up rules refuse before anything changes
// - Another tenant's invoice or document looks missing
// - The owner contact stays; private plans stay with sales
// - Public events carry wire codes, once per outbox message
//
// IMPORTANT NOTES:
// - Tests assert codes and states, never text (L-1)
// =========================================================
public sealed class PlatformTenantAccountUseCaseTests
{
    private readonly LicensingTestKit _kit = new();

    private static PlatformTenantAuthorizationContext As(Guid tenantId, PlatformTenantActorRole role, bool stepUp = true) => LicensingTestKit.Caller(tenantId, role, stepUp);

    private async Task<Guid> PendingDeviceAsync(Guid tenantId)
    {
        var response = await new EnrolPlatformDeviceUseCase(_kit.Administration()).ExecuteAsync(
            As(tenantId, PlatformTenantActorRole.Staff), new EnrolPlatformDeviceRequest { Name = "Dock scanner", Kind = PlatformDeviceKind.Scanner }, CancellationToken.None);
        Assert.True(response.Succeeded, response.Error?.Code);
        return response.Enrolment!.DeviceId;
    }

    [Fact]
    public async Task DeviceApproval_NeedsOwnerOrAdmin_WithASecondFactor_AndAnyRoleMayEnrol()
    {
        (PlatformTenant tenant, _) = _kit.Tenant();
        Guid device = await PendingDeviceAsync(tenant.Id);
        ApprovePlatformDeviceUseCase approve = new(_kit.Administration());

        Assert.Equal("platform.common.role_not_allowed", (await approve.ExecuteAsync(As(tenant.Id, PlatformTenantActorRole.Staff), new ApprovePlatformDeviceRequest { DeviceId = device }, CancellationToken.None)).Error?.Code);
        Assert.Equal("platform.common.role_not_allowed", (await approve.ExecuteAsync(As(tenant.Id, PlatformTenantActorRole.Billing), new ApprovePlatformDeviceRequest { DeviceId = device }, CancellationToken.None)).Error?.Code);
        Assert.Equal("platform.common.step_up_required", (await approve.ExecuteAsync(As(tenant.Id, PlatformTenantActorRole.Admin, stepUp: false), new ApprovePlatformDeviceRequest { DeviceId = device }, CancellationToken.None)).Error?.Code);
        Assert.True((await approve.ExecuteAsync(As(tenant.Id, PlatformTenantActorRole.Admin), new ApprovePlatformDeviceRequest { DeviceId = device }, CancellationToken.None)).Succeeded);

        // Only the owner asks for the first-device link.
        Assert.Equal("platform.common.role_not_allowed", (await new RequestPlatformFirstDeviceLinkUseCase(_kit.Administration()).ExecuteAsync(
            As(tenant.Id, PlatformTenantActorRole.Admin), new RequestPlatformFirstDeviceLinkRequest(), CancellationToken.None)).Error?.Code);
    }

    [Fact]
    public async Task TenantAccount_ShowsItsOwnFacts_AndKeepsTheOwnerContact()
    {
        (PlatformTenant tenant, _) = _kit.Tenant();
        var account = (await new GetTenantAccountUseCase(_kit.Kit.Tenants.Tenants, new PaymentsTestKit().Billing).ExecuteAsync(
            As(tenant.Id, PlatformTenantActorRole.Staff), new GetTenantAccountRequest(), CancellationToken.None)).Account!;
        Assert.Equal(tenant.TenantCode, account.TenantCode);
        Assert.Equal(tenant.CurrentSlug, account.Slug);
        Assert.Contains(account.Contacts, contact => contact.Role == PlatformTenantContactRole.Owner);

        UpdateTenantAccountUseCase update = new(_kit.Kit.Tenants.Tenants, _kit.Kit.Tenants.Store, _kit.Kit.Tenants.Audit(), _kit.Kit.Tenants.Clock);
        PlatformContactInput billing = new() { Role = PlatformTenantContactRole.Billing, Name = "Ana Billing", Email = "ana@tenant.test" };
        PlatformContactInput owner = new() { Role = PlatformTenantContactRole.Owner, Name = "Someone Else", Email = "else@tenant.test" };

        Assert.Equal("platform.common.step_up_required", (await update.ExecuteAsync(As(tenant.Id, PlatformTenantActorRole.Admin, stepUp: false),
            new UpdateTenantAccountRequest { Contacts = new[] { billing } }, CancellationToken.None)).Error?.Code);
        Assert.Equal("platform.tenant_account.owner_contact_locked", (await update.ExecuteAsync(As(tenant.Id, PlatformTenantActorRole.Owner),
            new UpdateTenantAccountRequest { Contacts = new[] { owner } }, CancellationToken.None)).Error?.Code);

        string ownerEmail = tenant.Contacts.Single(contact => contact.Role == PlatformTenantContactRole.Owner).Email.Value;
        Assert.True((await update.ExecuteAsync(As(tenant.Id, PlatformTenantActorRole.Admin),
            new UpdateTenantAccountRequest { Contacts = new[] { billing }, Language = "fr" }, CancellationToken.None)).Succeeded);
        Assert.Equal(ownerEmail, tenant.Contacts.Single(contact => contact.Role == PlatformTenantContactRole.Owner).Email.Value);
        Assert.Equal("ana@tenant.test", tenant.Contacts.Single(contact => contact.Role == PlatformTenantContactRole.Billing).Email.Value);
        Assert.Equal("fr", tenant.DefaultLanguage.Value);
    }

    [Fact]
    public async Task SubscriptionChange_ByTheTenant_IsLimitedToPublicPlansOrItsOwn_AndNeedsOwnerOrAdmin()
    {
        (PlatformTenant tenant, PlatformSubscription subscription) = _kit.Tenant(licences: 3);
        PlatformPlan hidden = _kit.Kit.Plan("PRIVATE", monthly: 5m, isPublic: false);
        ChangeTenantSubscriptionUseCase change = new(_kit.Kit.Subscriptions(), _kit.Kit.Tenants.Store);

        var options = (await new ListTenantPlanOptionsUseCase(_kit.Kit.Subscriptions()).ExecuteAsync(As(tenant.Id, PlatformTenantActorRole.Staff), new ListTenantPlanOptionsRequest(), CancellationToken.None)).Options;
        Assert.Contains(options, option => option.PlatformPlanId == subscription.PlatformPlanId);
        Assert.DoesNotContain(options, option => option.PlatformPlanId == hidden.Id);

        Assert.Equal("platform.common.role_not_allowed", (await change.ExecuteAsync(As(tenant.Id, PlatformTenantActorRole.Billing),
            new ChangeTenantSubscriptionRequest { PlatformPlanId = subscription.PlatformPlanId, Term = PlatformSubscriptionTerm.Monthly, Licences = 4 }, CancellationToken.None)).Error?.Code);
        Assert.Equal("platform.tenant_account.plan_not_offered", (await change.ExecuteAsync(As(tenant.Id, PlatformTenantActorRole.Owner),
            new ChangeTenantSubscriptionRequest { PlatformPlanId = hidden.Id, Term = PlatformSubscriptionTerm.Monthly, Licences = 3 }, CancellationToken.None)).Error?.Code);

        var done = await change.ExecuteAsync(As(tenant.Id, PlatformTenantActorRole.Owner),
            new ChangeTenantSubscriptionRequest { PlatformPlanId = subscription.PlatformPlanId, Term = PlatformSubscriptionTerm.Monthly, Licences = 5 }, CancellationToken.None);
        Assert.True(done.Succeeded, done.Error?.Code);
        Assert.Equal(5, done.Quote!.Licences);
        Assert.Contains(_kit.Kit.Tenants.Store.AuditEntries, entry => entry.ActionCode == "platform.subscriptions.changed" && entry.AfterJson!.Contains("ops-user-1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnotherTenantsInvoiceOrDocument_LooksMissing()
    {
        PaymentsTestKit payments = new();
        payments.Ready();
        (PlatformTenant tenant, PlatformInvoice invoice) = payments.OpenInvoice();
        PlatformTenant other = payments.Kit.Tenants.Tenant("other-" + Guid.NewGuid().ToString("N")[..6]);
        payments.Billing.AddDocument(PlatformBillingDocument.Create(PlatformBillingDocumentKind.Invoice, invoice.Id, invoice.Id, invoice.Number, LanguageCode.Default, new byte[] { 37, 80, 68, 70 }, payments.Now));
        CreateTenantPayLinkUseCase payLink = new(payments.Administration(), payments.ZelleAdministration());
        GetTenantBillingDocumentUseCase document = new(payments.BillingAdministration());

        Assert.Equal("platform.billing.invoice_not_found", (await payLink.ExecuteAsync(As(other.Id, PlatformTenantActorRole.Owner),
            new CreateTenantPayLinkRequest { PlatformInvoiceId = invoice.Id }, CancellationToken.None)).Error?.Code);
        Assert.Equal("platform.billing.document_not_found", (await document.ExecuteAsync(As(other.Id, PlatformTenantActorRole.Owner),
            new GetTenantBillingDocumentRequest { Kind = PlatformBillingDocumentKind.Invoice, OwnerId = invoice.Id }, CancellationToken.None)).Error?.Code);
        Assert.Equal("platform.common.role_not_allowed", (await payLink.ExecuteAsync(As(tenant.Id, PlatformTenantActorRole.Staff),
            new CreateTenantPayLinkRequest { PlatformInvoiceId = invoice.Id }, CancellationToken.None)).Error?.Code);

        var link = await payLink.ExecuteAsync(As(tenant.Id, PlatformTenantActorRole.Billing), new CreateTenantPayLinkRequest { PlatformInvoiceId = invoice.Id }, CancellationToken.None);
        Assert.True(link.Succeeded, link.Error?.Code);
        Assert.Contains("/pay/", link.Url, StringComparison.Ordinal);
        Assert.Equal(4, (await document.ExecuteAsync(As(tenant.Id, PlatformTenantActorRole.Staff),
            new GetTenantBillingDocumentRequest { Kind = PlatformBillingDocumentKind.Invoice, OwnerId = invoice.Id }, CancellationToken.None)).Content!.Length);
    }

    [Fact]
    public async Task PortalAccess_GivesTheCanonicalSlugAndLanguage_AndBatchesStopAt100()
    {
        (PlatformTenant tenant, _) = _kit.Tenant();
        var access = (await new GetPlatformPortalAccessUseCase(_kit.Administration()).ExecuteAsync(PlatformInternalAuthorizationContext.Instance,
            new GetPlatformPortalAccessRequest { Slug = tenant.CurrentSlug.ToUpperInvariant() }, CancellationToken.None)).Access!;
        Assert.True(access.Found);
        Assert.Equal(tenant.CurrentSlug, access.CanonicalSlug);
        Assert.Equal(tenant.DefaultLanguage.Value, access.DefaultLanguage);
        Assert.Equal(Vixly.Platform.Domain.Legal.PlatformLegalStatus.NotRequired, access.Legal);

        PlatformEntitlementsReader reader = new(_kit.Kit.Subscriptions(), _kit.Kit.Collections, _kit.Kit.Tenants.LegalGate());
        GetPlatformEntitlementsBatchUseCase batch = new(reader);
        Assert.Equal("platform.tenant_account.too_many", (await batch.ExecuteAsync(PlatformInternalAuthorizationContext.Instance,
            new GetPlatformEntitlementsBatchRequest { PlatformTenantIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToArray() }, CancellationToken.None)).Error?.Code);
        var items = (await batch.ExecuteAsync(PlatformInternalAuthorizationContext.Instance,
            new GetPlatformEntitlementsBatchRequest { PlatformTenantIds = new[] { tenant.Id, Guid.NewGuid() } }, CancellationToken.None)).Items;
        Assert.Equal(PlatformSubscriptionAccess.Full, items[0].Access);
        Assert.Equal(PlatformSubscriptionAccess.None, items[1].Access);
    }

    [Fact]
    public async Task PublicEvents_UseWireCodes_AreAddedOncePerMessage_AndTheFeedWaitsForTheSettleTime()
    {
        Guid tenantId = Guid.NewGuid();
        PlatformOutboxMessage message = PlatformOutboxMessage.Create(PlatformMessageTypes.DeviceRevoked, tenantId,
            JsonSerializer.Serialize(new PlatformDeviceChanged(tenantId, Guid.NewGuid(), "D-0001", PlatformDeviceStatus.Revoked, PlatformDeviceReason.LostOrStolen, _kit.Now), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            _kit.Now);
        FeedStore feed = new();
        FixedTimeProvider clock = new(_kit.Now);
        PlatformPublicEventHandler handler = new(PlatformMessageTypes.DeviceRevoked, feed, clock);

        await handler.HandleAsync(message, CancellationToken.None);
        await handler.HandleAsync(message, CancellationToken.None);

        Assert.Equal(new[] { PlatformEventTypes.DeviceRevoked, PlatformEventTypes.SeatsChanged }, feed.Items.Select(item => item.Type));
        using JsonDocument data = JsonDocument.Parse(feed.Items[0].Payload);
        Assert.Equal("revoked", data.RootElement.GetProperty("status").GetString());
        Assert.Equal("lost_or_stolen", data.RootElement.GetProperty("reason").GetString());
        Assert.Equal(PlatformDeviceReason.LostOrStolen, PlatformWireCodes.Parse<PlatformDeviceReason>("lost_or_stolen"));
        Assert.Null(PlatformWireCodes.Parse<PlatformDeviceReason>("LostOrStolen"));

        ReadPlatformEventsUseCase read = new(feed, clock);
        Assert.Empty((await read.ExecuteAsync(PlatformInternalAuthorizationContext.Instance, new ReadPlatformEventsRequest(), CancellationToken.None)).Items);

        clock.Advance(TimeSpan.FromSeconds(6));
        var page = await read.ExecuteAsync(PlatformInternalAuthorizationContext.Instance, new ReadPlatformEventsRequest { Limit = 1 }, CancellationToken.None);
        Assert.Single(page.Items);
        Assert.True(page.More);
        var rest = await read.ExecuteAsync(PlatformInternalAuthorizationContext.Instance, new ReadPlatformEventsRequest { After = page.Next, Types = PlatformEventTypes.ForEdge.ToArray() }, CancellationToken.None);
        Assert.Empty(rest.Items);
        Assert.Equal(page.Next, rest.Next);
    }

    private sealed class FeedStore : IPlatformPublishedEventRepository
    {
        public List<PlatformPublishedEvent> Items { get; } = new();

        public void Add(PlatformPublishedEvent item)
        {
            typeof(PlatformPublishedEvent).GetProperty(nameof(PlatformPublishedEvent.Sequence))!.SetValue(item, Items.Count + 1L);
            Items.Add(item);
        }

        public Task<bool> ExistsAsync(Guid outboxMessageId, string type, CancellationToken cancellationToken)
            => Task.FromResult(Items.Any(item => item.OutboxMessageId == outboxMessageId && item.Type == type));

        public Task<IReadOnlyList<PlatformPublishedEvent>> ListAfterAsync(long after, int take, DateTimeOffset createdBeforeUtc, IReadOnlyCollection<string> types, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PlatformPublishedEvent>>(Items.Where(item => item.Sequence > after && item.CreatedUtc < createdBeforeUtc && types.Contains(item.Type)).OrderBy(item => item.Sequence).Take(take).ToArray());

        public Task<int> DeleteCreatedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken)
            => Task.FromResult(Items.RemoveAll(item => item.CreatedUtc < cutoffUtc));
    }
}
