using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: LicensingTestKit.cs
//
// PURPOSE:
// Builds the licensing administration over the tenant, subscription and licensing fakes.
//
// RESPONSIBILITIES:
// - Create an active tenant with a paid subscription and a number of seats
// - Make the Operations caller context for a tenant
// - Deliver licensing emails through the outbox handler
//
// IMPORTANT NOTES:
// - Times come from the fixed clock
// =========================================================
internal sealed class LicensingTestKit
{
    public SubscriptionTestKit Kit { get; } = new();

    public FakeLicensingStore Store { get; } = new();

    public DateTimeOffset Now => Kit.Now;

    public PlatformLicensingAdministration Administration()
    {
        return new PlatformLicensingAdministration(
            Kit.Tenants.Gate(),
            new PlatformStepUpGuard(Kit.Tenants.Store, Kit.Tenants.Clock),
            Kit.Tenants.Store,
            Store,
            Store,
            Kit.Subscriptions(),
            Kit.Tenants.Tenants,
            Kit.Tenants.Tenants,
            Kit.Tenants.Tenants,
            Kit.Tenants.Crypto,
            Store,
            Kit.Sales,
            Kit.Tenants.Audit(),
            Kit.Tenants.Outbox(),
            Kit.Tenants.Store,
            Kit.Tenants.Clock,
            Kit.Tenants.LegalGate());
    }

    public (PlatformTenant Tenant, PlatformSubscription Subscription) Tenant(int licences = 3, bool paid = true)
    {
        PlatformTenant tenant = Kit.Tenants.Tenant("lic-" + Guid.NewGuid().ToString("N")[..6], active: true);
        PlatformSubscription subscription = Kit.Subscribe(tenant.Id, Kit.Plan(), paid, licences);
        return (tenant, subscription);
    }

    public static PlatformTenantAuthorizationContext Caller(Guid tenantId, PlatformTenantActorRole role = PlatformTenantActorRole.Owner, bool stepUp = true)
        => new(tenantId, "ops-user-1", "Grace Admin", role, stepUp, "operations", "198.51.100.4", "Mozilla/5.0");

    public async Task DeliverAsync()
    {
        PlatformLicensingNoticeHandler handler = new(Administration(), Store);

        foreach (PlatformOutboxMessage message in Kit.Tenants.Tenants.Outbox.Where(item => item.Type == PlatformMessageTypes.LicensingNotice && item.ProcessedUtc is null).ToArray())
        {
            await handler.HandleAsync(message, CancellationToken.None);
            message.MarkProcessed(Now);
        }
    }

    public IReadOnlyList<PlatformLicensingNoticeKind> Notices()
    {
        return Kit.Tenants.Tenants.Outbox.Where(item => item.Type == PlatformMessageTypes.LicensingNotice)
            .Select(item => PlatformOutboxWriter.Read<PlatformLicensingNotice>(item).Kind).ToArray();
    }
}
