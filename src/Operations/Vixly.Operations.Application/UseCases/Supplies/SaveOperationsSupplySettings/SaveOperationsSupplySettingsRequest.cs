using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupplySettings;

// =========================================================
// FILE: SaveOperationsSupplySettingsRequest.cs
//
// PURPOSE:
// The company's supplies stock settings (Slice 5E, S-9): count the stock or not, and what the counter does when an item has none left.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsSupplySettingsRequest
{
    public bool CountStock { get; init; } = true;

    public OperationsSupplyStockOut StockOut { get; init; } = OperationsSupplyStockOut.AllowWithWarning;

    // Slice 5E (S-17): the discount policy at the counter.
    public bool DiscountsAllowed { get; init; } = true;

    public decimal DiscountLinePercent { get; init; } = 10m;

    public decimal DiscountOrderPercent { get; init; } = 10m;

    public bool DiscountReasonRequired { get; init; } = true;
}
