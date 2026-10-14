using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformDiscountTests.cs
//
// PURPOSE:
// Proves discounts, one-time charges and the setup fee override on a subscription (D-S18 to D-S24).
//
// RESPONSIBILITIES:
// - Free months, percentage and fixed discounts, every duration
// - Approval, four eyes, one discount at a time
// - Charges and the setup fee
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// - A discount applies to periods that start while it is active
// =========================================================
public sealed class PlatformDiscountTests
{
    private static readonly DateTimeOffset Now = SubscriptionTestData.Now;

    private static readonly Guid Manager = Guid.Parse("0199e3a0-0000-7000-8000-000000000002");

    private static PlatformDiscountTerms Percent(decimal value, int? periods = null, DateTimeOffset? until = null)
    {
        PlatformDiscountDuration duration = periods is not null ? PlatformDiscountDuration.Periods : until is not null ? PlatformDiscountDuration.UntilDate : PlatformDiscountDuration.NoEnd;

        return PlatformDiscountTerms.Create(PlatformDiscountKind.Percent, value, duration, periods, until, Now);
    }

    [Theory]
    [InlineData(PlatformDiscountKind.Percent, 0, "platform.discounts.percent_invalid")]
    [InlineData(PlatformDiscountKind.Percent, 100.5, "platform.discounts.percent_invalid")]
    [InlineData(PlatformDiscountKind.Percent, 10.123, "platform.discounts.percent_invalid")]
    [InlineData(PlatformDiscountKind.FixedAmount, -1, "platform.discounts.amount_invalid")]
    public void Terms_RefuseBadValues(PlatformDiscountKind kind, double value, string code)
    {
        SubscriptionTestData.AssertCode(code, () => PlatformDiscountTerms.Create(kind, (decimal)value, PlatformDiscountDuration.NoEnd, null, null, Now));
    }

    [Fact]
    public void Terms_RefuseBadDurations()
    {
        SubscriptionTestData.AssertCode("platform.discounts.periods_invalid", () => PlatformDiscountTerms.Create(PlatformDiscountKind.Percent, 10m, PlatformDiscountDuration.Periods, 0, null, Now));
        SubscriptionTestData.AssertCode("platform.discounts.end_date_invalid", () => PlatformDiscountTerms.Create(PlatformDiscountKind.Percent, 10m, PlatformDiscountDuration.UntilDate, null, Now.AddDays(-1), Now));
    }

    [Fact]
    public void FirstMonthFree_StartsWithoutPayment_WhenNothingElseIsDue()
    {
        PlatformPlan plan = SubscriptionTestData.Published(terms: SubscriptionTestData.Terms(setup: 0m));
        PlatformSubscription subscription = SubscriptionTestData.Started(plan);

        PlatformSubscriptionEvent applied = subscription.RequestDiscount(Percent(100m, periods: 1), PlatformDiscountSource.Promotion, Guid.NewGuid(), "Launch campaign", false, null, Now);
        PlatformSubscriptionEvent? activated = subscription.ActivateIfNothingDue(plan.Current!, Now);

        Assert.Equal(PlatformSubscriptionEventCodes.DiscountApplied, applied.EventCode);
        Assert.Equal(PlatformSubscriptionEventCodes.ActivatedNothingDue, activated!.EventCode);
        Assert.Equal(PlatformSubscriptionStatus.Active, subscription.Status);
        Assert.Equal(30m, subscription.CurrentPeriodOff(30m));
        Assert.Equal(0m, subscription.NextPeriodOff(30m, Now));
    }

    [Fact]
    public void FirstMonthFree_StillWaitsForPayment_WhenASetupFeeIsDue()
    {
        PlatformPlan plan = SubscriptionTestData.Published();
        PlatformSubscription subscription = SubscriptionTestData.Started(plan);
        subscription.RequestDiscount(Percent(100m, periods: 1), PlatformDiscountSource.Staff, null, "Welcome", false, SubscriptionTestData.Actor, Now);

        Assert.Null(subscription.ActivateIfNothingDue(plan.Current!, Now));

        subscription.ChangeSetupFee(0m, plan.Current!, "Waived for launch", SubscriptionTestData.Actor, Now);

        Assert.NotNull(subscription.ActivateIfNothingDue(plan.Current!, Now));
    }

    [Fact]
    public void PercentForTwoPeriods_StartsNextPeriod_AndEndsAfterTwoRenewals()
    {
        PlatformPlan plan = SubscriptionTestData.Published();
        PlatformSubscription subscription = SubscriptionTestData.Active(plan);
        DateTimeOffset mid = Now.AddDays(10);

        subscription.RequestDiscount(Percent(20m, periods: 2), PlatformDiscountSource.Staff, null, "Valued customer", false, SubscriptionTestData.Actor, mid);

        Assert.Equal(0m, subscription.CurrentPeriodOff(30m));
        Assert.Equal(6m, subscription.NextPeriodOff(30m, mid));

        PlatformSubscriptionRenewal first = subscription.RenewIfDue(plan, plan, Now.AddMonths(1))!;
        PlatformSubscriptionRenewal second = subscription.RenewIfDue(plan, plan, Now.AddMonths(2))!;
        PlatformSubscriptionRenewal third = subscription.RenewIfDue(plan, plan, Now.AddMonths(3))!;

        Assert.Equal(6m, first.DiscountAmount);
        Assert.Equal(6m, second.DiscountAmount);
        Assert.Equal(0m, third.DiscountAmount);
        Assert.Equal(PlatformSubscriptionEventCodes.DiscountEnded, third.DiscountEnded!.EventCode);
        Assert.Null(subscription.ActiveDiscount);
        Assert.Equal(PlatformDiscountEndReason.Completed, subscription.Discounts.Single().EndReason);
    }

    [Fact]
    public void UntilDate_StopsAtTheFirstPeriodStartingOnOrAfterTheDate()
    {
        PlatformPlan plan = SubscriptionTestData.Published();
        PlatformSubscription subscription = SubscriptionTestData.Active(plan);
        subscription.RequestDiscount(Percent(50m, until: Now.AddMonths(2)), PlatformDiscountSource.Staff, null, "Pilot", false, SubscriptionTestData.Actor, Now.AddDays(1));

        Assert.Equal(15m, subscription.RenewIfDue(plan, plan, Now.AddMonths(1))!.DiscountAmount);
        Assert.Equal(0m, subscription.RenewIfDue(plan, plan, Now.AddMonths(2))!.DiscountAmount);
    }

    [Fact]
    public void FixedAmount_NeverTakesThePriceBelowZero()
    {
        PlatformDiscountTerms terms = PlatformDiscountTerms.Create(PlatformDiscountKind.FixedAmount, 100m, PlatformDiscountDuration.NoEnd, null, null, Now);

        Assert.Equal(30m, terms.Off(30m));
        Assert.Equal(0m, terms.Net(30m));
        Assert.Equal(12.5m, terms.ReduceCharge(12.5m));
    }

    [Fact]
    public void Upgrade_IsReducedByThePercentageOnTheCurrentPeriod()
    {
        PlatformPlan basic = SubscriptionTestData.Published();
        PlatformPlan growth = SubscriptionTestData.Published("GROWTH", SubscriptionTestData.Terms(monthly: 20m, annual: 200m));
        PlatformSubscription subscription = SubscriptionTestData.Started(basic);
        subscription.RequestDiscount(Percent(50m), PlatformDiscountSource.Staff, null, "Partner", false, SubscriptionTestData.Actor, Now);
        subscription.RecordOfflinePayment("Bank transfer", SubscriptionTestData.Actor, Now);

        PlatformSubscriptionQuote quote = subscription.Quote(basic, growth, PlatformSubscriptionTerm.Monthly, 3, 10, 0, Now);

        Assert.Equal(15m, quote.ChargeNow);
        Assert.Equal(60m, quote.NextPeriodAmount);
        Assert.Equal(30m, quote.NextPeriodNetAmount);
    }

    [Fact]
    public void OverTheLimit_WaitsForApproval_AndNobodyApprovesTheirOwn()
    {
        PlatformPlan plan = SubscriptionTestData.Published();
        PlatformSubscription subscription = SubscriptionTestData.Active(plan);
        subscription.RequestDiscount(Percent(10m), PlatformDiscountSource.Staff, null, "Loyalty", false, SubscriptionTestData.Actor, Now);
        PlatformSubscriptionEvent requested = subscription.RequestDiscount(Percent(40m), PlatformDiscountSource.Staff, null, "Big deal", true, SubscriptionTestData.Actor, Now);
        Guid pending = subscription.PendingDiscount!.Id;

        Assert.Equal(PlatformSubscriptionEventCodes.DiscountRequested, requested.EventCode);
        Assert.Equal(3m, subscription.NextPeriodOff(30m, Now));
        SubscriptionTestData.AssertCode("platform.discounts.already_pending", () => subscription.RequestDiscount(Percent(5m), PlatformDiscountSource.Staff, null, "Again", true, SubscriptionTestData.Actor, Now));
        SubscriptionTestData.AssertCode("platform.discounts.own_request", () => subscription.ApproveDiscount(pending, null, SubscriptionTestData.Actor, Now));

        subscription.ApproveDiscount(pending, "Agreed", Manager, Now);

        Assert.Equal(12m, subscription.NextPeriodOff(30m, Now));
        Assert.Equal(PlatformDiscountEndReason.Replaced, subscription.Discounts.First().EndReason);
        Assert.Single(subscription.Discounts, item => item.IsActive);
    }

    [Fact]
    public void Rejected_NeedsAReason_AndChangesNothing()
    {
        PlatformSubscription subscription = SubscriptionTestData.Active(SubscriptionTestData.Published());
        subscription.RequestDiscount(Percent(40m), PlatformDiscountSource.Staff, null, "Big deal", true, SubscriptionTestData.Actor, Now);
        Guid pending = subscription.PendingDiscount!.Id;

        SubscriptionTestData.AssertCode("platform.subscriptions.reason_required", () => subscription.RejectDiscount(pending, " ", Manager, Now));
        subscription.RejectDiscount(pending, "Too much", Manager, Now);

        Assert.Null(subscription.ActiveDiscount);
        Assert.Null(subscription.PendingDiscount);
    }

    [Fact]
    public void EndingTheSubscription_EndsItsDiscounts()
    {
        PlatformSubscription subscription = SubscriptionTestData.Active(SubscriptionTestData.Published());
        subscription.RequestDiscount(Percent(10m), PlatformDiscountSource.Staff, null, "Loyalty", false, SubscriptionTestData.Actor, Now);

        subscription.EndNow("Closed", SubscriptionTestData.Actor, Now);

        Assert.All(subscription.Discounts, item => Assert.Equal(PlatformDiscountEndReason.SubscriptionEnded, item.EndReason));
    }

    [Fact]
    public void Charges_AreAddedAndCancelledOnce()
    {
        PlatformSubscription subscription = SubscriptionTestData.Active(SubscriptionTestData.Published());

        SubscriptionTestData.AssertCode("platform.discounts.charge_description_required", () => subscription.AddCharge(" ", 10m, "USD", "Training", SubscriptionTestData.Actor, Now));
        subscription.AddCharge("On-site training", 500m, "USD", "Agreed in the contract", SubscriptionTestData.Actor, Now);
        Guid charge = subscription.Charges.Single().Id;
        subscription.CancelCharge(charge, "Done remotely", SubscriptionTestData.Actor, Now);

        Assert.Equal(PlatformOneTimeChargeStatus.Cancelled, subscription.Charges.Single().Status);
        SubscriptionTestData.AssertCode("platform.discounts.charge_not_pending", () => subscription.CancelCharge(charge, "Again", SubscriptionTestData.Actor, Now));
    }

    [Fact]
    public void SetupFee_CannotChangeOnceCharged()
    {
        PlatformPlan plan = SubscriptionTestData.Published();
        PlatformSubscription subscription = SubscriptionTestData.Active(plan);

        SubscriptionTestData.AssertCode("platform.discounts.setup_fee_charged", () => subscription.ChangeSetupFee(0m, plan.Current!, "Too late", SubscriptionTestData.Actor, Now));
    }
}
