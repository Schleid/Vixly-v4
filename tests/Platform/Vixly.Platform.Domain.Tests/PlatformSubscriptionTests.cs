using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformSubscriptionTests.cs
//
// PURPOSE:
// Proves the subscription model (D-S2 to D-S6, D-S10, D-S12, D-S14).
//
// RESPONSIBILITIES:
// - Start in a trial or awaiting payment, with the plan rules
// - Payments, trials, changes now or at the period end, renewal on the current version, cancellation
// - Reactions to tenant status and the access each status gives
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// - The subscription keeps its version for the whole period: the owner rule D-S2
// =========================================================
public sealed class PlatformSubscriptionTests
{
    private static readonly DateTimeOffset Now = SubscriptionTestData.Now;

    private static readonly DateTimeOffset MidPeriod = new(2026, 10, 16, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Start_WithATrial_EndsAfterTheTrialDays()
    {
        PlatformSubscriptionStart start = PlatformSubscription.Start(SubscriptionTestData.Tenant, SubscriptionTestData.Published(), PlatformSubscriptionTerm.Monthly, 3, 10, true, null, Now);

        Assert.Equal(PlatformSubscriptionStatus.Trialing, start.Subscription.Status);
        Assert.Equal(Now.AddDays(30), start.Subscription.TrialEndsUtc);
        Assert.Equal(PlatformSubscriptionEventCodes.StartedTrial, start.Event.EventCode);
        Assert.Equal("BASIC", start.Event.Arguments["plan"]);
    }

    [Fact]
    public void Start_WithoutATrial_AwaitsPayment()
    {
        PlatformSubscription subscription = SubscriptionTestData.Started(SubscriptionTestData.Published());

        Assert.Equal(PlatformSubscriptionStatus.PendingPayment, subscription.Status);
        Assert.Null(subscription.PeriodStartUtc);
    }

    [Fact]
    public void Start_AppliesThePlanRules()
    {
        PlatformPlan plan = SubscriptionTestData.Published(terms: SubscriptionTestData.Terms(annual: 0m, trialDays: null));

        SubscriptionTestData.AssertCode("platform.subscriptions.below_minimum", () => PlatformSubscription.Start(SubscriptionTestData.Tenant, plan, PlatformSubscriptionTerm.Monthly, 1, 10, false, null, Now));
        SubscriptionTestData.AssertCode("platform.subscriptions.above_limit", () => PlatformSubscription.Start(SubscriptionTestData.Tenant, plan, PlatformSubscriptionTerm.Monthly, 11, 10, false, null, Now));
        SubscriptionTestData.AssertCode("platform.subscriptions.term_not_offered", () => PlatformSubscription.Start(SubscriptionTestData.Tenant, plan, PlatformSubscriptionTerm.Annual, 3, 10, false, null, Now));
        SubscriptionTestData.AssertCode("platform.subscriptions.trial_not_offered", () => PlatformSubscription.Start(SubscriptionTestData.Tenant, plan, PlatformSubscriptionTerm.Monthly, 3, 10, true, null, Now));

        plan.Retire(Now);
        SubscriptionTestData.AssertCode("platform.subscriptions.plan_not_available", () => PlatformSubscription.Start(SubscriptionTestData.Tenant, plan, PlatformSubscriptionTerm.Monthly, 3, 10, false, null, Now));
    }

    [Fact]
    public void OfflinePayment_StartsTheFirstPeriod_AndChargesTheSetupFeeOnce()
    {
        PlatformSubscription subscription = SubscriptionTestData.Started(SubscriptionTestData.Published());

        PlatformSubscriptionEvent paid = subscription.RecordOfflinePayment("Wire transfer 1042", SubscriptionTestData.Actor, Now);

        Assert.Equal(PlatformSubscriptionStatus.Active, subscription.Status);
        Assert.Equal(Now, subscription.PeriodStartUtc);
        Assert.Equal(Now.AddMonths(1), subscription.PeriodEndUtc);
        Assert.Equal("true", paid.Arguments["setupFee"]);
        Assert.True(subscription.SetupFeeCharged);
        SubscriptionTestData.AssertCode("platform.subscriptions.not_awaiting_payment", () => subscription.RecordOfflinePayment("Again", SubscriptionTestData.Actor, Now));
        SubscriptionTestData.AssertCode("platform.subscriptions.reason_required", () => SubscriptionTestData.Started(SubscriptionTestData.Published()).RecordOfflinePayment(" ", SubscriptionTestData.Actor, Now));
    }

    [Fact]
    public void Trial_CanBeExtended_Ended_OrExpire()
    {
        PlatformPlan plan = SubscriptionTestData.Published();
        PlatformSubscription extended = SubscriptionTestData.Started(plan, trial: true);

        extended.ExtendTrial(Now.AddDays(45), "Pilot running late", SubscriptionTestData.Actor, Now.AddDays(1));
        Assert.Equal(Now.AddDays(45), extended.TrialEndsUtc);
        Assert.Null(extended.ExpireTrialIfDue(Now.AddDays(44)));
        Assert.NotNull(extended.ExpireTrialIfDue(Now.AddDays(45)));
        Assert.Equal(PlatformSubscriptionStatus.PendingPayment, extended.Status);

        PlatformSubscription ended = SubscriptionTestData.Started(plan, trial: true);
        ended.EndTrial("Customer ready to pay", SubscriptionTestData.Actor, Now.AddDays(3));
        Assert.Equal(PlatformSubscriptionStatus.PendingPayment, ended.Status);
        SubscriptionTestData.AssertCode("platform.subscriptions.not_trialing", () => ended.EndTrial("Again", SubscriptionTestData.Actor, Now.AddDays(3)));

        PlatformSubscription refused = SubscriptionTestData.Started(plan, trial: true);
        SubscriptionTestData.AssertCode("platform.subscriptions.trial_end_invalid", () => refused.ExtendTrial(Now.AddDays(-1), "Past", SubscriptionTestData.Actor, Now));
    }

    [Fact]
    public void ChoosingAPaidPlanDuringTheTrial_EndsTheTrialAtOnce()
    {
        PlatformPlan basic = SubscriptionTestData.Published();
        PlatformPlan growth = SubscriptionTestData.Published("GROWTH", SubscriptionTestData.Terms(monthly: 20m, trialDays: null));
        PlatformSubscription subscription = SubscriptionTestData.Started(basic, trial: true);

        PlatformSubscriptionQuote quote = subscription.Quote(basic, growth, PlatformSubscriptionTerm.Monthly, 3, 10, 0, Now.AddDays(5));
        subscription.Apply(quote, basic.Code, SubscriptionTestData.Actor, Now.AddDays(5));

        Assert.Equal(PlatformSubscriptionChangeTiming.Now, quote.Timing);
        Assert.True(quote.EndsTrial);
        Assert.Equal(0m, quote.ChargeNow);
        Assert.Equal(60m, quote.NextPeriodAmount);
        Assert.Equal(PlatformSubscriptionStatus.PendingPayment, subscription.Status);
        Assert.Equal(growth.Id, subscription.PlatformPlanId);
    }

    [Fact]
    public void Upgrade_AppliesNow_WithTheProratedDifference()
    {
        PlatformPlan basic = SubscriptionTestData.Published();
        PlatformPlan growth = SubscriptionTestData.Published("GROWTH", SubscriptionTestData.Terms(monthly: 20m));
        PlatformSubscription subscription = SubscriptionTestData.Active(basic);

        PlatformSubscriptionQuote quote = subscription.Quote(basic, growth, PlatformSubscriptionTerm.Monthly, 3, 10, 0, MidPeriod);
        PlatformSubscriptionEvent change = subscription.Apply(quote, basic.Code, SubscriptionTestData.Actor, MidPeriod);

        Assert.Equal(PlatformSubscriptionChangeTiming.Now, quote.Timing);
        Assert.Equal(15.48m, quote.ChargeNow);
        Assert.Equal(PlatformSubscriptionEventCodes.Changed, change.EventCode);
        Assert.Equal(growth.Current!.Id, subscription.PlanVersionId);
        Assert.Equal(Now.AddMonths(1), subscription.PeriodEndUtc);
    }

    [Fact]
    public void MoreLicences_ApplyNow_LikeV3CapacityExpansion()
    {
        PlatformPlan basic = SubscriptionTestData.Published();
        PlatformSubscription subscription = SubscriptionTestData.Active(basic);

        PlatformSubscriptionQuote quote = subscription.Quote(basic, basic, PlatformSubscriptionTerm.Monthly, 5, 10, 0, MidPeriod);

        Assert.Equal(PlatformSubscriptionChangeTiming.Now, quote.Timing);
        Assert.Equal(10.32m, quote.ChargeNow);
        Assert.Equal(50m, quote.NextPeriodAmount);
    }

    [Theory]
    [InlineData("downgrade")]
    [InlineData("fewer")]
    [InlineData("term")]
    public void DowngradesFewerLicencesAndTermChanges_WaitForThePeriodEnd(string kind)
    {
        PlatformPlan growth = SubscriptionTestData.Published("GROWTH", SubscriptionTestData.Terms(monthly: 20m, annual: 200m));
        PlatformPlan basic = SubscriptionTestData.Published();
        PlatformSubscription subscription = SubscriptionTestData.Active(growth, licences: 4);

        PlatformSubscriptionQuote quote = kind switch
        {
            "downgrade" => subscription.Quote(growth, basic, PlatformSubscriptionTerm.Monthly, 4, 10, 0, MidPeriod),
            "fewer" => subscription.Quote(growth, growth, PlatformSubscriptionTerm.Monthly, 3, 10, 0, MidPeriod),
            _ => subscription.Quote(growth, growth, PlatformSubscriptionTerm.Annual, 4, 10, 0, MidPeriod)
        };
        PlatformSubscriptionEvent scheduled = subscription.Apply(quote, growth.Code, SubscriptionTestData.Actor, MidPeriod);

        Assert.Equal(PlatformSubscriptionChangeTiming.AtPeriodEnd, quote.Timing);
        Assert.Equal(0m, quote.ChargeNow);
        Assert.Equal(Now.AddMonths(1), quote.EffectiveUtc);
        Assert.Equal(PlatformSubscriptionEventCodes.ChangeScheduled, scheduled.EventCode);
        Assert.Equal(growth.Id, subscription.PlatformPlanId);
        Assert.Equal(4, subscription.Licences);
        Assert.NotNull(subscription.ScheduledChange);

        subscription.CancelScheduledChange(SubscriptionTestData.Actor, MidPeriod);
        Assert.Null(subscription.ScheduledChange);
    }

    [Fact]
    public void Changes_RespectActiveLicences_AndRefuseNothing()
    {
        PlatformPlan basic = SubscriptionTestData.Published();
        PlatformSubscription subscription = SubscriptionTestData.Active(basic, licences: 5);

        SubscriptionTestData.AssertCode("platform.subscriptions.below_active_licences", () => subscription.Quote(basic, basic, PlatformSubscriptionTerm.Monthly, 3, 10, 4, MidPeriod));
        SubscriptionTestData.AssertCode("platform.subscriptions.nothing_to_change", () => subscription.Quote(basic, basic, PlatformSubscriptionTerm.Monthly, 5, 10, 0, MidPeriod));
    }

    [Fact]
    public void NewPrice_AppliesOnlyFromTheNextPeriod()
    {
        PlatformPlan basic = SubscriptionTestData.Published();
        PlatformSubscription subscription = SubscriptionTestData.Active(basic);
        Guid firstVersion = subscription.PlanVersionId;

        basic.StartDraft(MidPeriod);
        basic.EditDraft(SubscriptionTestData.Terms(monthly: 12m), MidPeriod);
        basic.PublishDraft(SubscriptionTestData.Actor, MidPeriod);

        Assert.Equal(firstVersion, subscription.PlanVersionId);
        Assert.Null(subscription.RenewIfDue(basic, null, MidPeriod));

        PlatformSubscriptionRenewal renewal = subscription.RenewIfDue(basic, null, Now.AddMonths(1))!;

        Assert.False(renewal.Ended);
        Assert.Equal(basic.Current!.Id, subscription.PlanVersionId);
        Assert.Equal(36m, renewal.PeriodAmount);
        Assert.Equal(Now.AddMonths(1), subscription.PeriodStartUtc);
        Assert.Equal(Now.AddMonths(2), subscription.PeriodEndUtc);
    }

    [Fact]
    public void Renewal_AppliesTheScheduledChange()
    {
        PlatformPlan growth = SubscriptionTestData.Published("GROWTH", SubscriptionTestData.Terms(monthly: 20m));
        PlatformPlan basic = SubscriptionTestData.Published();
        PlatformSubscription subscription = SubscriptionTestData.Active(growth, licences: 4);
        subscription.Apply(subscription.Quote(growth, basic, PlatformSubscriptionTerm.Monthly, 3, 10, 0, MidPeriod), growth.Code, SubscriptionTestData.Actor, MidPeriod);

        PlatformSubscriptionRenewal renewal = subscription.RenewIfDue(growth, basic, Now.AddMonths(1).AddHours(1))!;

        Assert.Equal(basic.Id, subscription.PlatformPlanId);
        Assert.Equal(3, subscription.Licences);
        Assert.Null(subscription.ScheduledChange);
        Assert.Equal(30m, renewal.PeriodAmount);
    }

    [Fact]
    public void Renewal_CatchesUpMissedPeriods()
    {
        PlatformPlan basic = SubscriptionTestData.Published();
        PlatformSubscription subscription = SubscriptionTestData.Active(basic);

        subscription.RenewIfDue(basic, null, Now.AddMonths(3).AddDays(2));

        Assert.Equal(Now.AddMonths(3), subscription.PeriodStartUtc);
        Assert.Equal(Now.AddMonths(4), subscription.PeriodEndUtc);
    }

    [Fact]
    public void Cancellation_WaitsForThePeriodEnd_AndCanBeResumed()
    {
        PlatformPlan basic = SubscriptionTestData.Published();
        PlatformSubscription subscription = SubscriptionTestData.Active(basic);

        subscription.RequestCancellation("Closing the warehouse", null, MidPeriod);
        Assert.Equal(PlatformSubscriptionStatus.Active, subscription.Status);
        Assert.True(subscription.CancelAtPeriodEnd);
        SubscriptionTestData.AssertCode("platform.subscriptions.cancellation_already_requested", () => subscription.RequestCancellation("Again", null, MidPeriod));

        subscription.Resume(SubscriptionTestData.Actor, MidPeriod);
        Assert.False(subscription.CancelAtPeriodEnd);

        subscription.RequestCancellation("Closing for real", null, MidPeriod);
        PlatformSubscriptionRenewal renewal = subscription.RenewIfDue(basic, null, Now.AddMonths(1))!;

        Assert.True(renewal.Ended);
        Assert.Equal(PlatformSubscriptionStatus.Ended, subscription.Status);
        Assert.Equal(PlatformSubscriptionEndReason.Cancelled, subscription.EndReason);
        Assert.Equal(Now.AddMonths(1), subscription.EndedUtc);
    }

    [Fact]
    public void CancellingATrial_EndsItAtOnce()
    {
        PlatformSubscription subscription = SubscriptionTestData.Started(SubscriptionTestData.Published(), trial: true);

        subscription.RequestCancellation("Not a fit", null, Now.AddDays(2));

        Assert.Equal(PlatformSubscriptionStatus.Ended, subscription.Status);
        Assert.False(subscription.IsCurrent);
    }

    [Fact]
    public void EndNow_NeedsAReason_AndIsFinal()
    {
        PlatformSubscription subscription = SubscriptionTestData.Active(SubscriptionTestData.Published());

        subscription.EndNow("Fraud", SubscriptionTestData.Actor, MidPeriod);

        Assert.Equal(PlatformSubscriptionEndReason.EndedByStaff, subscription.EndReason);
        SubscriptionTestData.AssertCode("platform.subscriptions.ended", () => subscription.EndNow("Again", SubscriptionTestData.Actor, MidPeriod));
        SubscriptionTestData.AssertCode("platform.subscriptions.ended", () => subscription.Resume(null, MidPeriod));
    }

    [Fact]
    public void TenantStatus_SuspendsRestoresAndEnds_Idempotently()
    {
        PlatformSubscription subscription = SubscriptionTestData.Active(SubscriptionTestData.Published());

        Assert.NotNull(subscription.SuspendWithTenant(MidPeriod));
        Assert.Null(subscription.SuspendWithTenant(MidPeriod));
        Assert.Equal(PlatformSubscriptionStatus.Suspended, subscription.Status);
        Assert.False(subscription.IsRenewalDue(Now.AddMonths(2)));
        SubscriptionTestData.AssertCode("platform.subscriptions.not_allowed_in_status", () => subscription.RequestCancellation("No", null, MidPeriod));

        Assert.NotNull(subscription.RestoreWithTenant(MidPeriod));
        Assert.Null(subscription.RestoreWithTenant(MidPeriod));
        Assert.Equal(PlatformSubscriptionStatus.Active, subscription.Status);

        Assert.NotNull(subscription.EndWithTenant(MidPeriod));
        Assert.Null(subscription.EndWithTenant(MidPeriod));
        Assert.Equal(PlatformSubscriptionEndReason.TenantClosed, subscription.EndReason);
    }

    [Fact]
    public void Access_FollowsTheStatus_WithAReadOnlyGraceWhenUnpaid()
    {
        PlatformSubscriptionAccessRules grace = new(TimeSpan.FromDays(7), TimeSpan.FromDays(7));
        PlatformPlan basic = SubscriptionTestData.Published();

        Assert.Equal(PlatformSubscriptionAccess.Full, SubscriptionTestData.Started(basic, trial: true).AccessAt(Now, grace));
        Assert.Equal(PlatformSubscriptionAccess.Full, SubscriptionTestData.Active(basic).AccessAt(Now, grace));

        PlatformSubscription unpaid = SubscriptionTestData.Started(basic);
        Assert.Equal(PlatformSubscriptionAccess.ReadOnly, unpaid.AccessAt(Now.AddDays(6), grace));
        Assert.Equal(PlatformSubscriptionAccess.PaymentRequired, unpaid.AccessAt(Now.AddDays(7), grace));

        PlatformSubscription suspended = SubscriptionTestData.Active(basic);
        suspended.SuspendWithTenant(Now);
        Assert.Equal(PlatformSubscriptionAccess.None, suspended.AccessAt(Now, grace));
    }
}
