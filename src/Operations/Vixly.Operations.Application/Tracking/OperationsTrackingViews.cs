using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Tracking;

// =========================================================
// FILE: OperationsTrackingViews.cs
//
// PURPOSE:
// Who sees which tracking events (ADR-0021 decision 5).
//
// RESPONSIBILITIES:
// - Staff: everything, voided events marked
// - Customer: corrected customer events about their own package, without staff details
// - Public: milestones and times only
//
// IMPORTANT NOTES:
// - Ownership and staff scope are checked by the use case before these run (S-3)
// =========================================================
public static class OperationsTrackingViews
{
    public static IReadOnlyList<OperationsTrackingLine> ForStaff(IEnumerable<OperationsTrackingEvent> events)
    {
        List<OperationsTrackingEvent> all = Ordered(events);
        HashSet<Guid> voided = Voided(all);
        return all.Select(item => Line(item, voided.Contains(item.Id), true)).ToList();
    }

    public static IReadOnlyList<OperationsTrackingLine> ForCustomer(IEnumerable<OperationsTrackingEvent> events)
    {
        List<OperationsTrackingEvent> all = Ordered(events);
        HashSet<Guid> voided = Voided(all);
        return all
            .Where(item => item.Visibility == OperationsTrackingVisibility.Customer && item.VoidsEventId is null && !voided.Contains(item.Id))
            .Select(item => Line(item, false, false))
            .ToList();
    }

    // Only the milestones the parcel really reached, so the timeline never contradicts its current milestone (review L1).
    public static IReadOnlyList<(OperationsMilestone Milestone, DateTimeOffset OccurredUtc)> ForPublic(IEnumerable<OperationsTrackingEvent> events)
    {
        List<(OperationsMilestone, DateTimeOffset)> reached = new();
        OperationsMilestone? current = null;

        foreach (OperationsTrackingLine line in ForCustomer(events).Where(line => line.Milestone is not null))
        {
            OperationsMilestone? next = OperationsTrackingProjection.Advance(current, line.Milestone);

            if (next != current)
            {
                reached.Add((next!.Value, line.OccurredUtc));
                current = next;
            }
        }

        return reached;
    }

    private static List<OperationsTrackingEvent> Ordered(IEnumerable<OperationsTrackingEvent> events) =>
        events.OrderBy(item => item.OccurredUtc).ThenBy(item => item.CreatedUtc).ThenBy(item => item.Id).ToList();

    private static HashSet<Guid> Voided(IEnumerable<OperationsTrackingEvent> events) =>
        events.Where(item => item.VoidsEventId is not null).Select(item => item.VoidsEventId!.Value).ToHashSet();

    // Customer lines keep only the arguments their code marks customer-safe, and no shelf or location code (review M1).
    private static OperationsTrackingLine Line(OperationsTrackingEvent item, bool voided, bool staff)
    {
        OperationsTrackingCode? code = OperationsTrackingCodes.Find(item.Code);
        IReadOnlyDictionary<string, string> arguments = item.Arguments();

        if (!staff)
        {
            arguments = arguments.Where(pair => code?.CustomerArguments.Contains(pair.Key, StringComparer.Ordinal) == true)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        }

        return new(item.Id, item.Code, item.OccurredUtc, item.TimeZone, staff ? item.LocationCode : null, arguments, code?.Milestone, voided,
            staff ? item.VoidsEventId : null, staff ? item.Note : null, staff ? item.Actor : null, staff ? item.ActorUserId : null);
    }
}
