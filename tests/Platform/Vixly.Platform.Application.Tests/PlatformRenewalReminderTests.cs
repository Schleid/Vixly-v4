using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformRenewalReminderTests.cs
//
// PURPOSE:
// Proves the reminder 7 days before an annual renewal charged to a saved card (2026-11-02).
//
// RESPONSIBILITIES:
// - Sent once per renewal, to the billing contact, naming the card and the statement wording
// - Not sent without automatic charging, for a monthly term, or too early
//
// IMPORTANT NOTES:
// - In-memory stores
// =========================================================
public sealed class PlatformRenewalReminderTests
{
    private readonly PaymentsTestKit _kit = new();

    private PlatformRenewalReminderClock Clock()
    {
        return new PlatformRenewalReminderClock(_kit.Kit.Store, _kit.Kit.Tenants.Tenants, _kit.Kit.Store, _kit.Administration(), _kit.Kit.Store, _kit.Kit.Tenants.Store);
    }

    private PlatformSubscription Annual(PlatformTenant tenant, int daysLeft, PlatformSubscriptionTerm term = PlatformSubscriptionTerm.Annual)
    {
        PlatformPlan plan = _kit.Kit.Plan("PRO" + Guid.NewGuid().ToString("N")[..4], 55m);
        DateTimeOffset started = term == PlatformSubscriptionTerm.Annual ? _kit.Now.AddDays(daysLeft).AddYears(-1) : _kit.Now.AddDays(daysLeft).AddMonths(-1);
        PlatformSubscription subscription = PlatformSubscription.Start(tenant.Id, plan, term, 10, 10, false, null, started).Subscription;
        subscription.RecordOfflinePayment("Wire", Guid.NewGuid(), started);
        _kit.Kit.Store.Subscriptions.Add(subscription);
        return subscription;
    }

    [Fact]
    public async Task Reminds_OncePerRenewal_WithTheCardAndTheStatementWording()
    {
        _kit.Ready();
        _kit.Payments.Merchant!.UpdateCheckout(null, null, null, null, null, "TECHCRAFT", "VIXLY", Guid.NewGuid(), _kit.Now);
        PlatformTenant tenant = _kit.Kit.Tenants.Tenant("galeo", active: true);
        PlatformSubscription subscription = Annual(tenant, 5);
        _kit.SaveCard(tenant.Id, autoCharge: true);

        int first = await Clock().RunAsync(CancellationToken.None);
        int second = await Clock().RunAsync(CancellationToken.None);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        var reminder = Assert.Single(_kit.Kit.Store.RenewalReminders);
        Assert.Equal("4242", reminder.Last4);
        Assert.Equal("TECHCRAFT* VIXLY", reminder.Statement);
        Assert.Equal(subscription.PeriodEndUtc, reminder.Renews);
        Assert.Equal(subscription.PeriodEndUtc, subscription.RenewalRemindedForUtc);
    }

    [Fact]
    public async Task NoReminder_WithoutAutomaticCharging_MonthlyOrTooEarly()
    {
        _kit.Ready();
        PlatformTenant manual = _kit.Kit.Tenants.Tenant("manual", active: true);
        Annual(manual, 5);
        _kit.SaveCard(manual.Id, autoCharge: false);
        PlatformTenant monthly = _kit.Kit.Tenants.Tenant("monthly", active: true);
        Annual(monthly, 5, PlatformSubscriptionTerm.Monthly);
        _kit.SaveCard(monthly.Id, autoCharge: true);
        PlatformTenant early = _kit.Kit.Tenants.Tenant("early", active: true);
        Annual(early, 20);
        _kit.SaveCard(early.Id, autoCharge: true);

        Assert.Equal(0, await Clock().RunAsync(CancellationToken.None));
        Assert.Empty(_kit.Kit.Store.RenewalReminders);
    }
}
