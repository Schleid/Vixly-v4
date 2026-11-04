namespace Vixly.Operations.Application.UseCases.Dispatch.ArriveOperationsDispatchLeg;

// =========================================================
// FILE: ArriveOperationsDispatchLegRequest.cs
//
// PURPOSE:
// Record that a leg arrived, at the time it did (Slice 6B, D-4 and D-5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ArriveOperationsDispatchLegRequest
{
    public Guid DispatchId { get; init; }

    public Guid LegId { get; init; }

    // Where the company works; none: now.
    public DateTime? At { get; init; }
}
