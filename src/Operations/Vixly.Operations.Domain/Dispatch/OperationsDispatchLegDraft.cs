namespace Vixly.Operations.Domain.Dispatch;

// =========================================================
// FILE: OperationsDispatchLegDraft.cs
//
// PURPOSE:
// What the dispatcher enters for a leg (Slice 6B, D-4).
//
// RESPONSIBILITIES:
// - Carry the kind, carrier, places, times and reference
//
// IMPORTANT NOTES:
// - Checked by OperationsDispatch.AddLeg
// =========================================================
public sealed record OperationsDispatchLegDraft(OperationsCarrierKind Kind, Guid? CarrierId, string? From, string? To, DateTimeOffset? DepartureUtc, DateTimeOffset? ArrivalUtc, string? Reference,
    string? Driver, string? Vehicle)
{
    // The form's key (ADR-0019): the same form posted twice adds one leg.
    public Guid? RequestKey { get; init; }
}
