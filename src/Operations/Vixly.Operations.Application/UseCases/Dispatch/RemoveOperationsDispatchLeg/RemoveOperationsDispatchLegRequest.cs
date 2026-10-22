namespace Vixly.Operations.Application.UseCases.Dispatch.RemoveOperationsDispatchLeg;

// =========================================================
// FILE: RemoveOperationsDispatchLegRequest.cs
//
// PURPOSE:
// Remove a leg of a dispatch before it leaves (Slice 6B, D-4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RemoveOperationsDispatchLegRequest
{
    public Guid DispatchId { get; init; }

    public Guid LegId { get; init; }
}
