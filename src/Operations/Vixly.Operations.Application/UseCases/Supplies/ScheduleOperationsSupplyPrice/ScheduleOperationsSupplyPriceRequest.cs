using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.ScheduleOperationsSupplyPrice;

// =========================================================
// FILE: ScheduleOperationsSupplyPriceRequest.cs
//
// PURPOSE:
// A new price for a supply, from now or a later day (Slice 5E, S-2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ScheduleOperationsSupplyPriceRequest
{
    public Guid SupplyId { get; init; }

    public decimal? Price { get; init; }

    // Quantity breaks: from this quantity, this unit price.
    public IReadOnlyList<OperationsSupplyBreak> Breaks { get; init; } = Array.Empty<OperationsSupplyBreak>();

    // Another unit price at a warehouse.
    public IReadOnlyList<OperationsSupplyWarehousePrice> WarehousePrices { get; init; } = Array.Empty<OperationsSupplyWarehousePrice>();

    // The day the price starts, in the company's time zone; none: now.
    public DateOnly? EffectiveFrom { get; init; }
}
