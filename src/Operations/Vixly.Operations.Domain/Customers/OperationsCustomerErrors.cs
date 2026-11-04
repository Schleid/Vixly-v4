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

    // Owner, 2026-10-31: a suspension may end by itself, on a day still to come.
    public static Error SuspendedUntilInvalid { get; } = Error.Create("ops.customers.suspended_until_invalid");

    // Owner, 2026-10-31: the email or phone belongs to a banned customer.
    public static Error BannedContact { get; } = Error.Create("ops.customers.banned_contact");

    public static Error ReasonRequired { get; } = Error.Create("ops.customers.reason_required");

    public static Error NotPending { get; } = Error.Create("ops.customers.not_pending");

    // Staff resending the confirmation email (2026-11-03): the corrected address is a work email.
    public static Error EmailIsStaff { get; } = Error.Create("ops.customers.email_is_staff");

    public static Error ConfirmationResendLimit { get; } = Error.Create("ops.customers.confirmation_resend_limit", ("limit", OperationsCustomerLimits.ConfirmationResendsPerHour));

    public static Error CodeInvalid { get; } = Error.Create("ops.customers.code_invalid");

    public static Error SessionEnded { get; } = Error.Create("ops.customers.session_ended");

    // Slice 5C: only an active customer ships from the counter.
    public static Error NotActive { get; } = Error.Create("ops.customers.not_active");

    // Review item 3: a work email (at any company) never becomes a customer email.
    public static Error EmailUnavailable { get; } = Error.Create("ops.customers.email_unavailable");

    public static Error LinkInvalid { get; } = Error.Create("ops.customers.link_invalid");

    // Owner, 2026-10-29 (design 4.4): too many customer lookups in the last hour.
    public static Error LookupLimit { get; } = Error.Create("ops.customers.lookup_limit", ("limit", Vixly.Operations.Domain.Access.OperationsAccessLimits.CustomerLookupsPerHour));

    // Vixly Lite (owner, 2026-10-30): the company's plan has no customer portal; staff enter customers themselves.
    public static Error PortalUnavailable { get; } = Error.Create("ops.customers.portal_unavailable");

    public static Error EnteredByStaffOnlyWithoutPortal { get; } = Error.Create("ops.customers.entry_needs_no_portal");
}
