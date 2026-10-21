namespace Vixly.Operations.Application.UseCases.Dispatch.CorrectOperationsDispatchCost;

// =========================================================
// FILE: CorrectOperationsDispatchCostRequest.cs
//
// PURPOSE:
// The dispatcher's cost for a dispatch, with a note (Slice 6B step 2, D-7): the carrier's invoice differs, or no rate was set.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CorrectOperationsDispatchCostRequest
{
    public Guid DispatchId { get; init; }

    // None: back to the worked-out cost.
    public decimal? Total { get; init; }

    public string? Note { get; init; }
}
