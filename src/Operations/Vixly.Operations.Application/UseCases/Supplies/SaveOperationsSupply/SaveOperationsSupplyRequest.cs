using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupply;

// =========================================================
// FILE: SaveOperationsSupplyRequest.cs
//
// PURPOSE:
// Create a shipping supply with its first price, or change its details and barcodes (Slice 5E, S-1 to S-3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsSupplyRequest
{
    // None: a new supply.
    public Guid? SupplyId { get; init; }

    public string? Code { get; init; }

    public string? NameEn { get; init; }

    public string? NameFr { get; init; }

    public string? NameEs { get; init; }

    public string? Unit { get; init; }

    public OperationsSupplyCategory Category { get; init; }

    public bool Taxed { get; init; } = true;

    public bool Active { get; init; } = true;

    public IReadOnlyList<string> Barcodes { get; init; } = Array.Empty<string>();

    // A new supply's first price; ignored when changing one (its prices have their own form).

    public decimal? Price { get; init; }

    // Quantity breaks: from this quantity, this unit price.
    public IReadOnlyList<OperationsSupplyBreak> Breaks { get; init; } = Array.Empty<OperationsSupplyBreak>();

    // Another unit price at a warehouse.
    public IReadOnlyList<OperationsSupplyWarehousePrice> WarehousePrices { get; init; } = Array.Empty<OperationsSupplyWarehousePrice>();

    // The day the price starts, in the company's time zone; none: now.
    public DateOnly? EffectiveFrom { get; init; }
}
