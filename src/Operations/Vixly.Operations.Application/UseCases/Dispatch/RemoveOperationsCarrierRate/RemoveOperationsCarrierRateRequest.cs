namespace Vixly.Operations.Application.UseCases.Dispatch.RemoveOperationsCarrierRate;

// =========================================================
// FILE: RemoveOperationsCarrierRateRequest.cs
//
// PURPOSE:
// Remove a rate that has not started yet (Slice 6B, D-1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RemoveOperationsCarrierRateRequest
{
    public Guid RateId { get; init; }
}
