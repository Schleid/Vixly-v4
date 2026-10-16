using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Collections;

// =========================================================
// FILE: PlatformCollectionCase.cs
//
// PURPOSE:
// One tenant's unpaid bills on their way through reminders, a final warning and the block (D-C1, D-C3, D-C7).
//
// RESPONSIBILITIES:
// - Work out the next step from the settings and the date, never two steps on one day
// - Move to a step and say what to record
// - Grant and end extensions within the limits
// - Close when nothing is overdue
//
// IMPORTANT NOTES:
// - At most one open case per tenant (filtered unique index); row version
// - The subscription status is changed by the caller in the same commit
// - Days count from the oldest unpaid due date when the case opened
// =========================================================
public sealed class PlatformCollectionCase : AuditableEntity
{
    private PlatformCollectionCase()
    {
    }

    private PlatformCollectionCase(Guid tenantId, Guid? subscriptionId, DateTimeOffset oldestDueUtc, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformTenantId = tenantId;
        PlatformSubscriptionId = subscriptionId;
        OldestDueUtc = oldestDueUtc;
        Status = PlatformCollectionCaseStatus.Open;
        Stage = PlatformCollectionStage.Opened;
        LastCheckedUtc = utcNow;
    }

    public Guid PlatformTenantId { get; private set; }

    public Guid? PlatformSubscriptionId { get; private set; }

    public PlatformCollectionCaseStatus Status { get; private set; }

    public PlatformCollectionStage Stage { get; private set; }

    public DateTimeOffset OldestDueUtc { get; private set; }

    public DateTimeOffset? PlannedBlockUtc { get; private set; }

    public DateTimeOffset? BlockedUtc { get; private set; }

    public DateTimeOffset? NoAccessUtc { get; private set; }

    public DateTimeOffset? ExtensionUntilUtc { get; private set; }

    public int ExtensionsUsed { get; private set; }

    public bool ExtensionEndingNoticeSent { get; private set; }

    public bool Held { get; private set; }

    public DateTimeOffset? LastStepUtc { get; private set; }

    public DateTimeOffset? LastCheckedUtc { get; private set; }

    public DateTimeOffset? ClosedUtc { get; private set; }

    public PlatformCollectionCloseReason? CloseReason { get; private set; }

    public bool IsOpen => Status == PlatformCollectionCaseStatus.Open;

    public bool IsBlocked => IsOpen && Stage == PlatformCollectionStage.Blocked;

    public bool InExtensionAt(DateTimeOffset utcNow) => ExtensionUntilUtc is { } until && until > utcNow;

    // The last day the extension covers (ExtensionUntilUtc is 00:00 UTC the day after).
    public DateTime? ExtendedThrough => ExtensionUntilUtc?.UtcDateTime.Date.AddDays(-1);

    public int DayAt(DateTimeOffset utcNow) => PlatformCollectionsLimits.DayOf(OldestDueUtc, utcNow);

    public static (PlatformCollectionCase Case, PlatformCollectionCaseEvent Event) Open(Guid tenantId, Guid? subscriptionId, DateTimeOffset oldestDueUtc, DateTimeOffset utcNow)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException(PlatformCollectionsErrors.CaseNotFound);
        }

        PlatformCollectionCase opened = new(tenantId, subscriptionId, oldestDueUtc, utcNow);

        return (opened, PlatformCollectionCaseEvent.Record(opened, PlatformCollectionEventCodes.Opened, null, utcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal) { ["due"] = PlatformCollectionCaseEvent.Stamp(oldestDueUtc) }));
    }

    // =====================================================
    // METHOD: TIMELINE (D-C3)
    // =====================================================
    public PlatformCollectionStep? NextStep(PlatformCollectionsSettings settings, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!IsOpen || Held || InExtensionAt(utcNow) || Stage == PlatformCollectionStage.Blocked)
        {
            return null;
        }

        // Never two steps on the same UTC day (catch-up after downtime sends only the latest).
        if (LastStepUtc is { } last && last.UtcDateTime.Date == utcNow.UtcDateTime.Date)
        {
            return null;
        }

        if (Stage == PlatformCollectionStage.FinalWarning)
        {
            return PlannedBlockUtc is { } block && utcNow >= block ? PlatformCollectionStep.Block : null;
        }

        int day = DayAt(utcNow);

        if (day >= settings.FinalWarningDay)
        {
            return PlatformCollectionStep.FinalWarning;
        }

        if (day >= settings.Reminder2Day && Stage < PlatformCollectionStage.Reminder2)
        {
            return PlatformCollectionStep.Reminder2;
        }

        if (day >= settings.Reminder1Day && Stage < PlatformCollectionStage.Reminder1)
        {
            return PlatformCollectionStep.Reminder1;
        }

        return null;
    }

    public PlatformCollectionCaseEvent Advance(PlatformCollectionStep step, PlatformCollectionsSettings settings, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(settings);
        RequireOpen();

        Dictionary<string, string> arguments = new(StringComparer.Ordinal) { ["day"] = DayAt(utcNow).ToString(System.Globalization.CultureInfo.InvariantCulture) };
        string code;

        switch (step)
        {
            case PlatformCollectionStep.Reminder1:
                Stage = PlatformCollectionStage.Reminder1;
                code = PlatformCollectionEventCodes.Reminder1;
                break;
            case PlatformCollectionStep.Reminder2:
                Stage = PlatformCollectionStage.Reminder2;
                code = PlatformCollectionEventCodes.Reminder2;
                break;
            case PlatformCollectionStep.FinalWarning:
                // The block always follows the final warning by at least the minimum notice.
                DateTimeOffset planned = PlatformCollectionsLimits.StartOfDay(OldestDueUtc, settings.BlockDay);
                DateTimeOffset earliest = new(utcNow.UtcDateTime.Date.AddDays(settings.MinimumNoticeDays), TimeSpan.Zero);
                PlannedBlockUtc = planned > earliest ? planned : earliest;
                Stage = PlatformCollectionStage.FinalWarning;
                code = PlatformCollectionEventCodes.FinalWarning;
                arguments["blockDate"] = PlatformCollectionCaseEvent.Stamp(PlannedBlockUtc.Value);
                break;
            case PlatformCollectionStep.Block:
                Stage = PlatformCollectionStage.Blocked;
                BlockedUtc = utcNow;
                NoAccessUtc = null;
                code = PlatformCollectionEventCodes.Blocked;
                break;
            default:
                throw new DomainException(PlatformCollectionsErrors.SettingsInvalid);
        }

        LastStepUtc = utcNow;
        MarkAsModified(utcNow);

        return PlatformCollectionCaseEvent.Record(this, code, null, utcNow, arguments: arguments);
    }

    // The read-only days after the block have passed: record it once (D-C4).
    public PlatformCollectionCaseEvent? RecordNoAccessIfDue(PlatformCollectionsSettings settings, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!IsBlocked || NoAccessUtc is not null || BlockedUtc is not { } blocked || settings.BlockedReadOnlyDays is not int days || utcNow < blocked.AddDays(days))
        {
            return null;
        }

        NoAccessUtc = utcNow;
        MarkAsModified(utcNow);

        return PlatformCollectionCaseEvent.Record(this, PlatformCollectionEventCodes.NoAccess, null, utcNow);
    }

    public void Hold(bool held, DateTimeOffset utcNow)
    {
        if (Held != held)
        {
            Held = held;
            MarkAsModified(utcNow);
        }
    }

    public void Checked(DateTimeOffset utcNow)
    {
        LastCheckedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: CARD RETRIES (D-C9)
    // =====================================================
    public static string RetryKeyFor(Guid invoiceId, int day)
    {
        return invoiceId.ToString("N") + ":" + day.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public PlatformCollectionCaseEvent RecordRetry(Guid invoiceId, string invoiceNumber, int day, DateTimeOffset utcNow)
    {
        RequireOpen();
        MarkAsModified(utcNow);

        return PlatformCollectionCaseEvent.Record(this, PlatformCollectionEventCodes.CardRetry, null, utcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["invoice"] = invoiceNumber,
                ["day"] = day.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            retryKey: RetryKeyFor(invoiceId, day));
    }

    // =====================================================
    // METHOD: EXTENSIONS (D-C7)
    // =====================================================
    public PlatformCollectionExtension GrantExtension(
        DateTimeOffset untilUtc,
        string? reason,
        bool mayExceedLimit,
        PlatformCollectionsSettings settings,
        Guid actorUserId,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(settings);
        RequireOpen();

        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformCollectionsErrors.ActorRequired);
        }

        string why = Reason(reason);

        // "Until 20 October" includes that day: the extension ends at 00:00 UTC the day after.
        DateTime through = untilUtc.UtcDateTime.Date;
        DateTimeOffset until = new(through.AddDays(1), TimeSpan.Zero);

        if (through < utcNow.UtcDateTime.Date)
        {
            throw new DomainException(PlatformCollectionsErrors.ExtensionDateInvalid);
        }

        if (through > utcNow.UtcDateTime.Date.AddDays(settings.ExtensionMaxDays))
        {
            throw new DomainException(PlatformCollectionsErrors.ExtensionTooLong(settings.ExtensionMaxDays));
        }

        if (ExtensionsUsed >= settings.ExtensionsPerCase && !mayExceedLimit)
        {
            throw new DomainException(PlatformCollectionsErrors.ExtensionLimit(settings.ExtensionsPerCase));
        }

        bool wasBlocked = Stage == PlatformCollectionStage.Blocked;

        if (wasBlocked)
        {
            Stage = PlatformCollectionStage.FinalWarning;
            BlockedUtc = null;
            NoAccessUtc = null;
        }

        PlannedBlockUtc = null;
        ExtensionUntilUtc = until;
        ExtensionsUsed++;
        ExtensionEndingNoticeSent = false;
        MarkAsModified(utcNow);

        PlatformCollectionCaseEvent recorded = PlatformCollectionCaseEvent.Record(this, PlatformCollectionEventCodes.ExtensionGranted, actorUserId, utcNow, why,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["through"] = Day(through),
                ["count"] = ExtensionsUsed.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });

        return new PlatformCollectionExtension(recorded, wasBlocked);
    }

    // True once, when the extension ends within the notice period: send "extension ends soon".
    public bool TakeExtensionEndingNotice(DateTimeOffset utcNow)
    {
        if (!IsOpen || ExtensionUntilUtc is not { } until || ExtensionEndingNoticeSent || until - utcNow > PlatformCollectionsLimits.ExtensionEndingNotice || until <= utcNow)
        {
            return false;
        }

        ExtensionEndingNoticeSent = true;
        MarkAsModified(utcNow);
        return true;
    }

    // An extension that ran out unpaid resumes at the final warning, with a new block date.
    public PlatformCollectionCaseEvent? EndExtensionIfDue(DateTimeOffset utcNow)
    {
        if (!IsOpen || ExtensionUntilUtc is not { } until || until > utcNow)
        {
            return null;
        }

        ExtensionUntilUtc = null;
        ExtensionEndingNoticeSent = false;

        if (Stage >= PlatformCollectionStage.FinalWarning)
        {
            Stage = PlatformCollectionStage.Reminder2;
        }

        PlannedBlockUtc = null;
        LastStepUtc = null;
        MarkAsModified(utcNow);

        return PlatformCollectionCaseEvent.Record(this, PlatformCollectionEventCodes.ExtensionEnded, null, utcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal) { ["through"] = Day(until.UtcDateTime.Date.AddDays(-1)) });
    }

    // =====================================================
    // METHOD: NOTES AND CHECKS (D-C8)
    // =====================================================
    public PlatformCollectionCaseEvent AddNote(string? note, Guid actorUserId, DateTimeOffset utcNow)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformCollectionsErrors.ActorRequired);
        }

        string text = note?.Trim() ?? string.Empty;

        if (text.Length == 0 || text.Length > PlatformCollectionsLimits.NoteMaxLength)
        {
            throw new DomainException(PlatformCollectionsErrors.NoteInvalid);
        }

        MarkAsModified(utcNow);

        return PlatformCollectionCaseEvent.Record(this, PlatformCollectionEventCodes.Note, actorUserId, utcNow, text);
    }

    public PlatformCollectionCaseEvent RecordCheck(Guid actorUserId, DateTimeOffset utcNow)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformCollectionsErrors.ActorRequired);
        }

        Checked(utcNow);

        return PlatformCollectionCaseEvent.Record(this, PlatformCollectionEventCodes.Checked, actorUserId, utcNow);
    }

    // =====================================================
    // METHOD: CLOSE (D-C1, D-C6)
    // =====================================================
    public IReadOnlyList<PlatformCollectionCaseEvent> Close(PlatformCollectionCloseReason reason, DateTimeOffset utcNow)
    {
        RequireOpen();

        List<PlatformCollectionCaseEvent> events = new();

        if (Stage == PlatformCollectionStage.Blocked && reason == PlatformCollectionCloseReason.Settled)
        {
            events.Add(PlatformCollectionCaseEvent.Record(this, PlatformCollectionEventCodes.Unblocked, null, utcNow));
        }

        Status = PlatformCollectionCaseStatus.Closed;
        CloseReason = reason;
        ClosedUtc = utcNow;
        ExtensionUntilUtc = null;
        PlannedBlockUtc = null;
        MarkAsModified(utcNow);

        events.Add(PlatformCollectionCaseEvent.Record(this, PlatformCollectionEventCodes.Closed, null, utcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal) { ["reason"] = reason.ToString() }));

        return events;
    }

    private static string Day(DateTime value)
    {
        return value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    }

    private void RequireOpen()
    {
        if (!IsOpen)
        {
            throw new DomainException(PlatformCollectionsErrors.CaseClosed);
        }
    }

    private static string Reason(string? reason)
    {
        string text = reason?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            throw new DomainException(PlatformCollectionsErrors.ReasonRequired);
        }

        if (text.Length > PlatformCollectionsLimits.ReasonMaxLength)
        {
            throw new DomainException(PlatformCollectionsErrors.NoteInvalid);
        }

        return text;
    }
}
