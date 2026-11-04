using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplySaleErrors.cs
//
// PURPOSE:
// Error codes of supplies sold at the counter (Slice 5E step 3).
//
// RESPONSIBILITIES:
// - Name every refusal about a sale, its lines and its discounts
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsSupplySaleErrors
{
    public static Error NotFound { get; } = Error.Create("ops.supplies.sale.not_found");

    public static Error NotOpen { get; } = Error.Create("ops.supplies.sale.not_open");

    public static Error ShipmentFinal { get; } = Error.Create("ops.supplies.sale.shipment_final");

    public static Error Empty { get; } = Error.Create("ops.supplies.sale.empty");

    public static Error LineNotFound { get; } = Error.Create("ops.supplies.sale.line_not_found");

    public static Error TooManyLines { get; } = Error.Create("ops.supplies.sale.too_many_lines");

    public static Error NoPrice { get; } = Error.Create("ops.supplies.sale.no_price");

    public static Error NotFoundByScan { get; } = Error.Create("ops.supplies.sale.not_found_by_scan");

    public static Error DiscountNotAllowed { get; } = Error.Create("ops.supplies.sale.discount_not_allowed");

    public static Error DiscountInvalid { get; } = Error.Create("ops.supplies.sale.discount_invalid");

    public static Error DiscountReasonRequired { get; } = Error.Create("ops.supplies.sale.discount_reason_required");

    public static Error DiscountNeedsApproval { get; } = Error.Create("ops.supplies.sale.discount_needs_approval");

    public static Error ApproverInvalid { get; } = Error.Create("ops.supplies.sale.approver_invalid");
}
