using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupplyCount;

// =========================================================
// FILE: SaveOperationsSupplyCountRequest.cs
//
// PURPOSE:
// A stock count saved as the clerk goes, closed (each difference becomes a count correction) or cancelled (Slice 5E, S-7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsSupplyCountRequest
{
    public Guid CountId { get; init; }

    // The quantities entered; null: not counted.
    public IReadOnlyDictionary<Guid, int?> Counted { get; init; } = new Dictionary<Guid, int?>();

    // Close: apply the differences; cancel: close without changing the stock.
    public bool Close { get; init; }

    public bool Cancel { get; init; }
}
