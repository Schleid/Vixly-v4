namespace Vixly.Operations.Domain.Tracking;

// =========================================================
// FILE: OperationsTrackingProjection.cs
//
// PURPOSE:
// Turns a package's events into its state (ADR-0021 decision 3).
//
// RESPONSIBILITIES:
// - Replay events in the order they happened, without voided ones
// - Apply the milestone order rules of ADR-0018 decision 2
//
// IMPORTANT NOTES:
// - The same function serves every write, so the stored state always equals a full replay
// - Late offline scans are placed by their occurred time, not their arrival
// =========================================================
public static class OperationsTrackingProjection
{
    public static OperationsParcelState Replay(IEnumerable<OperationsTrackingEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        List<OperationsTrackingEvent> all = events.ToList();
        HashSet<Guid> voided = all.Where(item => item.VoidsEventId is not null).Select(item => item.VoidsEventId!.Value).ToHashSet();

        OperationsMilestone? milestone = null;
        OperationsParcelFlags flags = OperationsParcelFlags.None;
        OperationsHoldReasons holds = OperationsHoldReasons.None;
        DateTimeOffset? last = null;

        foreach (OperationsTrackingEvent item in all
            .Where(item => item.VoidsEventId is null && !voided.Contains(item.Id))
            .OrderBy(item => item.OccurredUtc)
            .ThenBy(item => item.CreatedUtc)
            .ThenBy(item => item.Id))
        {
            OperationsTrackingCode? code = OperationsTrackingCodes.Find(item.Code);

            if (code is null)
            {
                continue;
            }

            milestone = Advance(milestone, code.Milestone);
            flags = (flags & ~code.Clears) | code.Sets;

            if (code.OpensHold)
            {
                holds |= item.HoldReason();
            }
            else if (code.ReleasesHold)
            {
                holds &= ~item.HoldReason();
            }

            // Slice 6C: a note, a delay or an exception changes nothing in the parcel's state, so its last event stays (review: the holds queue sorts by it).
            if (!OperationsTrackingCodes.ManualCodes.Contains(code.Name, StringComparer.Ordinal))
            {
                last = item.OccurredUtc;
            }
        }

        flags = holds == OperationsHoldReasons.None ? flags & ~OperationsParcelFlags.OnHold : flags | OperationsParcelFlags.OnHold;
        return new OperationsParcelState(milestone, flags, holds, last);
    }

    public static bool IsTerminal(OperationsMilestone milestone) =>
        milestone is OperationsMilestone.Cancelled or OperationsMilestone.ReturnedToSender or OperationsMilestone.Disposed;

    // A terminal milestone is final; otherwise a package only moves forward. Received and booked
    // may come in either order, and the higher of the two holds (ADR-0018 decision 2).
    public static OperationsMilestone? Advance(OperationsMilestone? current, OperationsMilestone? reached)
    {
        if (reached is not { } next)
        {
            return current;
        }

        if (current is { } now && IsTerminal(now))
        {
            return now;
        }

        if (IsTerminal(next) || current is null)
        {
            return next;
        }

        return next > current ? next : current;
    }
}
