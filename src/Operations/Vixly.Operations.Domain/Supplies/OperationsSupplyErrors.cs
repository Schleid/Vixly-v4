using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyErrors.cs
//
// PURPOSE:
// Error codes of the supplies catalogue and its prices (Slice 5E).
//
// RESPONSIBILITIES:
// - Name every refusal about supplies
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsSupplyErrors
{
    public static Error NotFound { get; } = Error.Create("ops.supplies.not_found");

    public static Error NameRequired { get; } = Error.Create("ops.supplies.name_required");

    public static Error CodeRequired { get; } = Error.Create("ops.supplies.code_required");

    public static Error CodeInvalid { get; } = Error.Create("ops.supplies.code_invalid");

    public static Error CodeDuplicate { get; } = Error.Create("ops.supplies.code_duplicate");

    public static Error TextInvalid { get; } = Error.Create("ops.supplies.text_invalid");

    public static Error CategoryInvalid { get; } = Error.Create("ops.supplies.category_invalid");

    public static Error BarcodeInvalid { get; } = Error.Create("ops.supplies.barcode_invalid");

    public static Error BarcodeDuplicate { get; } = Error.Create("ops.supplies.barcode_duplicate");

    public static Error PriceInvalid { get; } = Error.Create("ops.supplies.price_invalid");

    public static Error BreaksInvalid { get; } = Error.Create("ops.supplies.breaks_invalid");

    public static Error WarehousePricesInvalid { get; } = Error.Create("ops.supplies.warehouse_prices_invalid");

    public static Error TooMany { get; } = Error.Create("ops.supplies.too_many");
}
