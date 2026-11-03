using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.TenantAccount.ReportTenantShipmentUsage;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformShipmentUsageUseCaseTests.cs
//
// PURPOSE:
// Checks the monthly shipment counts from Operations and the charge of the shipments above the cap (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - A running month is only recorded; a closed month charges extra shipments times the plan's price, once
// - Under the cap, or without a cap, a closed month is settled without a charge
// - An override raises the cap used for the charge
//
// IMPORTANT NOTES:
// - The kit's clock is 2026-10-14, so 2026-09 is closed and 2026-10 is running
// =========================================================
public sealed class PlatformShipmentUsageUseCaseTests
{
    private readonly SubscriptionTestKit _kit = new();

    [Fact]
    public async Task ClosedMonth_ChargesTheExtraShipmentsOnce()
    {
        (Guid tenantId, PlatformSubscription subscription) = Capped(150, 0.40m);

        Assert.True((await Report(tenantId, "2026-10", 400, isFinal: true)).Succeeded);
        Assert.Empty(subscription.PendingCharges);
        Assert.False(_kit.Store.Usage.Single(item => item.Month == "2026-10").IsFinal);

        Assert.True((await Report(tenantId, "2026-09", 200, isFinal: true)).Succeeded);
        Assert.True((await Report(tenantId, "2026-09", 260, isFinal: true)).Succeeded);

        PlatformOneTimeCharge charge = Assert.Single(subscription.PendingCharges);
        Assert.Equal(20m, charge.Amount);
        PlatformTenantShipmentUsage september = _kit.Store.Usage.Single(item => item.Month == "2026-09");
        Assert.Equal(50, september.ExtraShipments);
        Assert.Equal(200, september.Shipments);
    }

    [Fact]
    public async Task UnderTheCap_OrWithAnOverride_NothingIsCharged()
    {
        (Guid tenantId, PlatformSubscription subscription) = Capped(150, 0.40m);
        _kit.Store.Overrides.Add(PlatformTenantLimitOverride.Set(tenantId, PlatformPlanLimitKind.MaxShipmentsPerMonth, 300, "Busy season", null, Guid.NewGuid(), _kit.Now.AddDays(-40)));

        Assert.True((await Report(tenantId, "2026-09", 250, isFinal: true)).Succeeded);

        Assert.Empty(subscription.PendingCharges);
        Assert.Equal(0, _kit.Store.Usage.Single().ExtraShipments);
        Assert.Equal(300, _kit.Store.Usage.Single().Cap);
        Assert.Equal("platform.subscriptions.usage_month_invalid", (await Report(tenantId, "september", 1, isFinal: false)).Error?.Code);
    }

    private (Guid TenantId, PlatformSubscription Subscription) Capped(int cap, decimal price)
    {
        PlatformPlan plan = PlatformPlan.Create("LITE", "Lite plan", null, "USD", Guid.NewGuid(), _kit.Now.AddDays(-60));
        plan.EditDraft(PlatformPlanVersionTerms.Create("USD", 25m, 250m, 200m, 1, null, 2, 2, null).WithShipmentCap(cap, price), _kit.Now.AddDays(-60));
        plan.PublishDraft(Guid.NewGuid(), _kit.Now.AddDays(-60));
        _kit.Store.Plans.Add(plan);
        Guid tenantId = _kit.Tenants.Tenant("lite", active: true).Id;
        return (tenantId, _kit.Subscribe(tenantId, plan, licences: 1));
    }

    private Task<ReportTenantShipmentUsageResponse> Report(Guid tenantId, string month, int shipments, bool isFinal) =>
        new ReportTenantShipmentUsageUseCase(_kit.Subscriptions(), _kit.Store, _kit.Tenants.Store).ExecuteAsync(
            new PlatformTenantAuthorizationContext(tenantId, "ops", "Operations"),
            new ReportTenantShipmentUsageRequest { Month = month, Shipments = shipments, IsFinal = isFinal },
            CancellationToken.None);
}
