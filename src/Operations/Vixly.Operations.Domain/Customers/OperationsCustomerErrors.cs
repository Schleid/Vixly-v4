using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Customers;

// =========================================================
// FILE: OperationsCustomerErrors.cs
//
// PURPOSE:
// Error codes of customers (Slice 2).
//
// RESPONSIBILITIES:
// - Name every refusal about a customer
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsCustomerErrors
{
    public static Error NameRequired { get; } = Error.Create("ops.customers.name_required");

    public static Error NameTooLong { get; } = Error.Create("ops.customers.name_too_long", ("maxLength", OperationsCustomerLimits.NameMaxLength));

    // Angle brackets or a web or email address: names go into emails, so they must not carry a link (review).
    public static Error NameInvalid { get; } = Error.Create("ops.customers.name_invalid");

    public static Error CompanyTooLong { get; } = Error.Create("ops.customers.company_too_long", ("maxLength", OperationsCustomerLimits.CompanyMaxLength));

    public static Error PhoneInvalid { get; } = Error.Create("ops.customers.phone_invalid");

    public static Error StatusChangeInvalid { get; } = Error.Create("ops.customers.status_change_invalid");

    public static Error ReasonRequired { get; } = Error.Create("ops.customers.reason_required");

    public static Error NotPending { get; } = Error.Create("ops.customers.not_pending");

    public static Error CodeInvalid { get; } = Error.Create("ops.customers.code_invalid");

    public static Error SessionEnded { get; } = Error.Create("ops.customers.session_ended");

    // Slice 5C: only an active customer ships from the counter.
    public static Error NotActive { get; } = Error.Create("ops.customers.not_active");

    public static Error LinkInvalid { get; } = Error.Create("ops.customers.link_invalid");
}
