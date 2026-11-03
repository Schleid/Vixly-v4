using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Payables;

// =========================================================
// FILE: OperationsPayableErrors.cs
//
// PURPOSE:
// Error codes of bills, vendors and payments made (Slice 8).
//
// RESPONSIBILITIES:
// - Name each refusal
//
// IMPORTANT NOTES:
// - Codes only; texts in OperationsText (en, fr, es)
// =========================================================
public static class OperationsPayableErrors
{
    public static Error NotFound { get; } = Error.Create("ops.bills.not_found");

    public static Error VendorInvalid { get; } = Error.Create("ops.bills.vendor_invalid");

    public static Error VendorNameTaken { get; } = Error.Create("ops.bills.vendor_name_taken");

    public static Error PartyRequired { get; } = Error.Create("ops.bills.party_required");

    public static Error ReferenceRequired { get; } = Error.Create("ops.bills.reference_required");

    public static Error ReferenceTaken { get; } = Error.Create("ops.bills.reference_taken");

    public static Error AmountInvalid { get; } = Error.Create("ops.bills.amount_invalid");

    public static Error DatesInvalid { get; } = Error.Create("ops.bills.dates_invalid");

    public static Error AllocationInvalid { get; } = Error.Create("ops.bills.allocation_invalid");

    public static Error AllocationTooHigh { get; } = Error.Create("ops.bills.allocation_too_high");

    public static Error AllocationNotFound { get; } = Error.Create("ops.bills.allocation_not_found");

    public static Error NotEditable { get; } = Error.Create("ops.bills.not_editable");

    public static Error NotApprovable { get; } = Error.Create("ops.bills.not_approvable");

    public static Error OwnBill { get; } = Error.Create("ops.bills.own_bill");

    public static Error ReasonRequired { get; } = Error.Create("ops.bills.reason_required");

    public static Error NotPayable { get; } = Error.Create("ops.bills.not_payable");

    public static Error PaymentTooHigh { get; } = Error.Create("ops.bills.payment_too_high");

    public static Error NotVoidable { get; } = Error.Create("ops.bills.not_voidable");

    public static Error TextInvalid { get; } = Error.Create("ops.bills.text_invalid");

    public static Error ShareNotAllowed { get; } = Error.Create("ops.bills.share_not_allowed");

    public static Error CarrierNotOnDispatch { get; } = Error.Create("ops.bills.carrier_not_on_dispatch");

    public static Error Changed { get; } = Error.Create("ops.bills.changed");
}
