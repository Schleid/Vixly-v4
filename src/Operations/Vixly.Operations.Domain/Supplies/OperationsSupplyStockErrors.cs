using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyStockErrors.cs
//
// PURPOSE:
// Error codes of the supplies stock (Slice 5E step 2).
//
// RESPONSIBILITIES:
// - Name every refusal about stock, transfers and counts
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsSupplyStockErrors
{
    public static Error QuantityInvalid { get; } = Error.Create("ops.supplies.stock.quantity_invalid");

    public static Error CostInvalid { get; } = Error.Create("ops.supplies.stock.cost_invalid");

    public static Error ReasonRequired { get; } = Error.Create("ops.supplies.stock.reason_required");

    public static Error LinesInvalid { get; } = Error.Create("ops.supplies.stock.lines_invalid");

    public static Error WarehouseInvalid { get; } = Error.Create("ops.supplies.stock.warehouse_invalid");

    public static Error NotCounting { get; } = Error.Create("ops.supplies.stock.not_counting");

    public static Error OutOfStock { get; } = Error.Create("ops.supplies.stock.out_of_stock");

    public static Error TransferNotFound { get; } = Error.Create("ops.supplies.stock.transfer_not_found");

    public static Error TransferDone { get; } = Error.Create("ops.supplies.stock.transfer_done");

    public static Error CountNotFound { get; } = Error.Create("ops.supplies.stock.count_not_found");

    public static Error CountOpen { get; } = Error.Create("ops.supplies.stock.count_open");

    public static Error CountClosed { get; } = Error.Create("ops.supplies.stock.count_closed");

    public static Error ChoiceInvalid { get; } = Error.Create("ops.supplies.stock.choice_invalid");
}
