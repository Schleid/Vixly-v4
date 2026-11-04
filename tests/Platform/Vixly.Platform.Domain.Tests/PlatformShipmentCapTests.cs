using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformShipmentCapTests.cs
//
// PURPOSE:
// Checks the plan's monthly shipment cap, the extra-shipment charge and the monthly counts (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Cap and price kept on the version; no cap clears the price; versions without it have no cap
// - The charge is the extra shipments times the price, rounded to the cent, with no staff member named
// - A month is charged once and its count no longer changes
//
// IMPORTANT NOTES:
// - Uses SubscriptionTestData
// =========================================================
public sealed class PlatformShipmentCapTests
{
    [Fact]
    public void Version_KeepsTheCapAndPrice_AndNoCapClearsThePrice()
    {
        PlatformPlan capped = SubscriptionTestData.Published("LITE", SubscriptionTestData.Terms().WithShipmentCap(150, 0.40m));
        Assert.Equal(150, capped.Current!.Limit(PlatformPlanLimitKind.MaxShipmentsPerMonth));
        Assert.Equal(0.40m, capped.Current.ExtraShipmentPrice);
        Assert.Equal(150, capped.Current.Terms.MaxShipmentsPerMonth);

        PlatformPlan open = SubscriptionTestData.Published("OPEN", SubscriptionTestData.Terms().WithShipmentCap(0, 0.40m));
        Assert.Equal(0, open.Current!.Limit(PlatformPlanLimitKind.MaxShipmentsPerMonth));
        Assert.Equal(0m, open.Current.ExtraShipmentPrice);
        Assert.DoesNotContain(open.Current.Limits, limit => limit.Kind == PlatformPlanLimitKind.MaxShipmentsPerMonth);

        SubscriptionTestData.AssertCode("platform.plans.limit_invalid", () => SubscriptionTestData.Terms().WithShipmentCap(-1, 0m));
        SubscriptionTestData.AssertCode("platform.plans.price_invalid", () => SubscriptionTestData.Terms().WithShipmentCap(100, -1m));
    }

    [Fact]
    public void ShipmentCharge_IsExtraTimesPrice_RoundedToTheCent()
    {
        PlatformSubscription subscription = SubscriptionTestData.Active(SubscriptionTestData.Published());

        PlatformSubscriptionEvent change = subscription.AddShipmentCharge("2026-10", 75, 0.333m, "USD", SubscriptionTestData.Now);

        PlatformOneTimeCharge charge = Assert.Single(subscription.PendingCharges);
        Assert.Equal(24.98m, charge.Amount);
        Assert.Null(charge.AddedByUserId);
        Assert.Contains("2026-10", charge.Description, StringComparison.Ordinal);
        Assert.Equal(PlatformSubscriptionEventCodes.ShipmentsCharged, change.EventCode);
        Assert.Equal("75", change.Arguments["extra"]);
        SubscriptionTestData.AssertCode("platform.discounts.amount_invalid", () => subscription.AddShipmentCharge("2026-10", 0, 1m, "USD", SubscriptionTestData.Now));
    }

    [Fact]
    public void Usage_IsChargedOnce_AndTheMonthMustBeValid()
    {
        PlatformTenantShipmentUsage usage = PlatformTenantShipmentUsage.Start(SubscriptionTestData.Tenant, "2026-10", SubscriptionTestData.Now);
        usage.Report(120, false, SubscriptionTestData.Now);
        usage.Report(180, true, SubscriptionTestData.Now);
        Assert.True(usage.IsFinal);

        usage.MarkCharged(150, 30, 12m, "USD", SubscriptionTestData.Now);
        usage.Report(500, true, SubscriptionTestData.Now);

        Assert.Equal(180, usage.Shipments);
        Assert.Equal(30, usage.ExtraShipments);
        SubscriptionTestData.AssertCode("platform.subscriptions.usage_already_charged", () => usage.MarkCharged(150, 30, 12m, "USD", SubscriptionTestData.Now));
        SubscriptionTestData.AssertCode("platform.subscriptions.usage_month_invalid", () => PlatformTenantShipmentUsage.Start(SubscriptionTestData.Tenant, "2026-13", SubscriptionTestData.Now));
        SubscriptionTestData.AssertCode("platform.subscriptions.usage_count_invalid", () => PlatformTenantShipmentUsage.Start(SubscriptionTestData.Tenant, "2026-11", SubscriptionTestData.Now).Report(-1, false, SubscriptionTestData.Now));
    }
}
