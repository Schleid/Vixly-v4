using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.UseCases.Events.GetOperationsEventTimeline;

// =========================================================
// FILE: GetOperationsEventTimelineRequest.cs
//
// PURPOSE:
// One subject's history in the events workspace (Slice 6C): its events newest first, its parcels, and what can be added.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsEventTimelineRequest
{
    public OperationsTrackingSubject Kind { get; init; }

    public Guid Id { get; init; }

    // all, customer (what the customer sees), manual.
    public string? Filter { get; init; }

    public int Page { get; init; } = 1;
}
