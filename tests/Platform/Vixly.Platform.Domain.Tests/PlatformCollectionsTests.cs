using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformCollectionsTests.cs
//
// PURPOSE:
// Proves the collections rules of Slice 6: settings, the 14-day timeline, the block levels, extensions and clearing.
//
// RESPONSIBILITIES:
// - Timeline: reminders, final warning with notice, block; never two steps on one day; catch-up sends the latest step only
// - Block levels: read-only for the set days, then payment required; customers are outside this model
// - Extensions: inclusive date, limits, lifting a block, resuming after
// - Subscription: blocked from Active, Past due or Pending payment; paying clears it
//
// IMPORTANT NOTES:
// - Tests assert codes and states, never text (L-1)
// =========================================================
public sealed class PlatformCollectionsTests
{
    private static readonly DateTimeOffset Due = new(2026, 10, 1, 15, 0, 0, TimeSpan.Zero);

    private static readonly Guid Tenant = Guid.Parse("0199f100-0000-7000-8000-0000000000aa");

    private static readonly Guid Actor = Guid.Parse("0199f100-0000-7000-8000-000000000001");

    private static DateTimeOffset Day(int day, int hour = 6) => new(Due.UtcDateTime.Date.AddDays(day).AddHours(hour), TimeSpan.Zero);

    private static PlatformCollectionsSettings Settings() => PlatformCollectionsSettings.Create(Due);

    private static PlatformCollectionCase Opened() => PlatformCollectionCase.Open(Tenant, null, Due, Day(1)).Case;

    private static void AssertCode(string code, Action act)
    {
        DomainException error = Assert.Throws<DomainException>(act);
        Assert.Equal(code, error.Error.Code);
    }

    [Fact]
    public void Settings_StartWithTheOwnersTimeline_AndRefuseStepsOutOfOrder()
    {
        PlatformCollectionsSettings settings = Settings();

        Assert.True(settings.Enabled);
        Assert.Equal((1, 5, 10, 14), (settings.Reminder1Day, settings.Reminder2Day, settings.FinalWarningDay, settings.BlockDay));
        Assert.Equal(new[] { 3, 7 }, settings.RetryDayList);
        Assert.Equal(7, settings.BlockedReadOnlyDays);
        Assert.Equal((14, 2), (settings.ExtensionMaxDays, settings.ExtensionsPerCase));

        AssertCode("platform.collections.settings_invalid", () => settings.Update(true, 5, 5, 10, 14, new[] { 3 }, 3, 7, 14, 2, 7, 60, Actor, Due));
        AssertCode("platform.collections.settings_invalid", () => settings.Update(true, 1, 5, 12, 14, new[] { 3 }, 3, 7, 14, 2, 7, 60, Actor, Due));
        AssertCode("platform.collections.settings_invalid", () => settings.Update(true, 1, 5, 10, 61, new[] { 3 }, 3, 7, 14, 2, 7, 60, Actor, Due));
        AssertCode("platform.collections.settings_invalid", () => settings.Update(true, 1, 5, 10, 14, new[] { 14 }, 3, 7, 14, 2, 7, 60, Actor, Due));
        AssertCode("platform.collections.actor_required", () => settings.Update(true, 1, 5, 10, 14, new[] { 3 }, 3, 7, 14, 2, 7, 60, Guid.Empty, Due));

        settings.Update(true, 1, 7, 14, 21, new[] { 7, 3, 3 }, 3, null, 14, 2, 7, 60, Actor, Due);
        Assert.Equal("3,7", settings.RetryDays);
        Assert.Null(settings.AccessRules.BlockedReadOnly);
    }

    [Fact]
    public void Timeline_RemindsWarnsAndBlocks_OnTheSetDays_OneStepPerDay()
    {
        PlatformCollectionsSettings settings = Settings();
        PlatformCollectionCase collection = Opened();

        Assert.Equal(1, collection.DayAt(Day(1)));
        Assert.Equal(PlatformCollectionStep.Reminder1, collection.NextStep(settings, Day(1)));
        collection.Advance(PlatformCollectionStep.Reminder1, settings, Day(1));
        Assert.Null(collection.NextStep(settings, Day(1, 20)));
        Assert.Null(collection.NextStep(settings, Day(4)));

        Assert.Equal(PlatformCollectionStep.Reminder2, collection.NextStep(settings, Day(5)));
        collection.Advance(PlatformCollectionStep.Reminder2, settings, Day(5));

        Assert.Equal(PlatformCollectionStep.FinalWarning, collection.NextStep(settings, Day(10)));
        PlatformCollectionCaseEvent warning = collection.Advance(PlatformCollectionStep.FinalWarning, settings, Day(10));
        Assert.Equal(Day(14, 0), collection.PlannedBlockUtc);
        Assert.Equal(PlatformCollectionEventCodes.FinalWarning, warning.EventCode);

        Assert.Null(collection.NextStep(settings, Day(13, 23)));
        Assert.Equal(PlatformCollectionStep.Block, collection.NextStep(settings, Day(14)));
        collection.Advance(PlatformCollectionStep.Block, settings, Day(14));
        Assert.True(collection.IsBlocked);
        Assert.Null(collection.NextStep(settings, Day(30)));
    }

    [Fact]
    public void CatchUp_SendsOnlyTheLatestStep_AndNeverBlocksWithoutNotice()
    {
        PlatformCollectionsSettings settings = Settings();
        PlatformCollectionCase collection = Opened();

        // Down until day 13: the final warning goes out, never reminders or a block the same day.
        Assert.Equal(PlatformCollectionStep.FinalWarning, collection.NextStep(settings, Day(13)));
        collection.Advance(PlatformCollectionStep.FinalWarning, settings, Day(13));

        Assert.Equal(Day(16, 0), collection.PlannedBlockUtc);
        Assert.Null(collection.NextStep(settings, Day(14)));
        Assert.Equal(PlatformCollectionStep.Block, collection.NextStep(settings, Day(16)));
    }

    [Fact]
    public void Block_TurnsToNoAccess_AfterTheReadOnlyDays_OnlyOnce()
    {
        PlatformCollectionsSettings settings = Settings();
        PlatformCollectionCase collection = Opened();
        collection.Advance(PlatformCollectionStep.FinalWarning, settings, Day(10));
        collection.Advance(PlatformCollectionStep.Block, settings, Day(14));

        Assert.Null(collection.RecordNoAccessIfDue(settings, Day(20)));
        Assert.NotNull(collection.RecordNoAccessIfDue(settings, Day(21)));
        Assert.Null(collection.RecordNoAccessIfDue(settings, Day(22)));
    }

    [Fact]
    public void HeldCase_DoesNotMove_UntilTheCreditRequestIsDecided()
    {
        PlatformCollectionsSettings settings = Settings();
        PlatformCollectionCase collection = Opened();

        collection.Hold(true, Day(1));
        Assert.Null(collection.NextStep(settings, Day(10)));
        collection.Hold(false, Day(10));
        Assert.Equal(PlatformCollectionStep.FinalWarning, collection.NextStep(settings, Day(10)));
    }

    [Fact]
    public void Extension_IncludesItsLastDay_RespectsLimits_AndLiftsABlock()
    {
        PlatformCollectionsSettings settings = Settings();
        PlatformCollectionCase collection = Opened();
        collection.Advance(PlatformCollectionStep.FinalWarning, settings, Day(10));
        collection.Advance(PlatformCollectionStep.Block, settings, Day(14));

        AssertCode("platform.collections.reason_required", () => collection.GrantExtension(Day(20), " ", false, settings, Actor, Day(15)));
        AssertCode("platform.collections.extension_too_long", () => collection.GrantExtension(Day(30), "Cheque in the post", false, settings, Actor, Day(15)));
        AssertCode("platform.collections.extension_date_invalid", () => collection.GrantExtension(Day(14), "Cheque in the post", false, settings, Actor, Day(15)));

        PlatformCollectionExtension first = collection.GrantExtension(Day(20), "Cheque in the post", false, settings, Actor, Day(15));
        Assert.True(first.LiftedBlock);
        Assert.False(collection.IsBlocked);
        Assert.Equal(Day(20).UtcDateTime.Date, collection.ExtendedThrough);
        Assert.True(collection.InExtensionAt(Day(20, 23)));
        Assert.False(collection.InExtensionAt(Day(21, 0)));
        Assert.Null(collection.NextStep(settings, Day(20)));

        Assert.False(collection.TakeExtensionEndingNotice(Day(17)));
        Assert.True(collection.TakeExtensionEndingNotice(Day(19)));
        Assert.False(collection.TakeExtensionEndingNotice(Day(20)));

        Assert.NotNull(collection.EndExtensionIfDue(Day(21)));
        Assert.Equal(PlatformCollectionStep.FinalWarning, collection.NextStep(settings, Day(21)));
        collection.Advance(PlatformCollectionStep.FinalWarning, settings, Day(21));
        Assert.Equal(Day(24, 0), collection.PlannedBlockUtc);

        collection.GrantExtension(Day(22), "Promise to pay", false, settings, Actor, Day(21));
        AssertCode("platform.collections.extension_limit", () => collection.GrantExtension(Day(25), "Third", false, settings, Actor, Day(22)));
        Assert.Equal(3, collection.GrantExtension(Day(25), "Third, approved", true, settings, Actor, Day(22)).Event.Arguments["count"] switch { "3" => 3, _ => 0 });
    }

    [Fact]
    public void Close_RecordsUnblocked_WhenABlockedCaseIsSettled()
    {
        PlatformCollectionsSettings settings = Settings();
        PlatformCollectionCase collection = Opened();
        collection.Advance(PlatformCollectionStep.FinalWarning, settings, Day(10));
        collection.Advance(PlatformCollectionStep.Block, settings, Day(14));

        IReadOnlyList<PlatformCollectionCaseEvent> events = collection.Close(PlatformCollectionCloseReason.Settled, Day(15));

        Assert.Equal(new[] { PlatformCollectionEventCodes.Unblocked, PlatformCollectionEventCodes.Closed }, events.Select(item => item.EventCode));
        Assert.False(collection.IsOpen);
        AssertCode("platform.collections.case_closed", () => collection.Close(PlatformCollectionCloseReason.Settled, Day(16)));
        AssertCode("platform.collections.note_invalid", () => Opened().AddNote(new string('x', 1001), Actor, Day(2)));
    }

    [Fact]
    public void RetryKey_IsOnePerInvoiceAndDay()
    {
        Guid invoice = Guid.Parse("0199f100-0000-7000-8000-0000000000bb");
        PlatformCollectionCaseEvent retry = Opened().RecordRetry(invoice, "INV-2026-000001", 3, Day(3));

        Assert.Equal(PlatformCollectionCase.RetryKeyFor(invoice, 3), retry.RetryKey);
        Assert.NotEqual(PlatformCollectionCase.RetryKeyFor(invoice, 3), PlatformCollectionCase.RetryKeyFor(invoice, 7));
    }

    [Fact]
    public void Subscription_IsBlockedForNonPayment_ReadOnlyThenPaymentRequired_AndPayingClearsIt()
    {
        PlatformPlan plan = SubscriptionTestData.Published();
        PlatformSubscription subscription = SubscriptionTestData.Active(plan);
        PlatformSubscriptionAccessRules rules = new(TimeSpan.FromDays(7), TimeSpan.FromDays(7));
        DateTimeOffset now = SubscriptionTestData.Now.AddDays(40);

        subscription.MarkPastDue("INV-2026-000001", now);
        PlatformSubscriptionEvent? blocked = subscription.BlockForNonPayment("INV-2026-000001", now);

        Assert.NotNull(blocked);
        Assert.Equal(PlatformSubscriptionStatus.BlockedForNonPayment, subscription.Status);
        Assert.Equal(PlatformSubscriptionStatus.PastDue, subscription.StatusBeforeBlock);
        Assert.Null(subscription.BlockForNonPayment(null, now));
        Assert.Equal(PlatformSubscriptionAccess.ReadOnly, subscription.AccessAt(now.AddDays(6), rules));
        Assert.Equal(PlatformSubscriptionAccess.PaymentRequired, subscription.AccessAt(now.AddDays(7), rules));
        Assert.Equal(PlatformSubscriptionAccess.ReadOnly, subscription.AccessAt(now.AddDays(70), new PlatformSubscriptionAccessRules(TimeSpan.FromDays(7), null)));

        Assert.Null(subscription.InvoicePaid("INV-2026-000001", otherInvoicesOverdue: true, now));
        Assert.NotNull(subscription.InvoicePaid("INV-2026-000001", otherInvoicesOverdue: false, now));
        Assert.Equal(PlatformSubscriptionStatus.Active, subscription.Status);
        Assert.Null(subscription.StatusBeforeBlock);
        Assert.Null(subscription.BlockedUtc);
    }

    [Fact]
    public void Subscription_ExtensionLiftsTheBlock_AndSuspensionReturnsToBlocked()
    {
        PlatformPlan plan = SubscriptionTestData.Published();
        PlatformSubscription subscription = SubscriptionTestData.Started(plan);
        DateTimeOffset now = SubscriptionTestData.Now.AddDays(9);

        subscription.BlockForNonPayment(null, now);
        subscription.SuspendWithTenant(now);
        subscription.RestoreWithTenant(now);
        Assert.Equal(PlatformSubscriptionStatus.BlockedForNonPayment, subscription.Status);

        Assert.NotNull(subscription.LiftBlockForExtension(now.UtcDateTime.Date.AddDays(7), SubscriptionTestData.Actor, now));
        Assert.Equal(PlatformSubscriptionStatus.PendingPayment, subscription.Status);
    }
}
