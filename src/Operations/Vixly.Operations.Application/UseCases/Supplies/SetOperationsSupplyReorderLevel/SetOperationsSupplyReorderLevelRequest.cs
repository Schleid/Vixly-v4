using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.SetOperationsSupplyReorderLevel;

// =========================================================
// FILE: SetOperationsSupplyReorderLevelRequest.cs
//
// PURPOSE:
// The stock level at which a supply shows as low at a warehouse (Slice 5E, S-8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SetOperationsSupplyReorderLevelRequest
{
    public Guid WarehouseId { get; init; }

    public Guid SupplyId { get; init; }

    // None: never shown as low.
    public int? Level { get; init; }
}
