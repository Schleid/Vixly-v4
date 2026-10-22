using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Company;

// =========================================================
// FILE: OperationsCompanyErrors.cs
//
// PURPOSE:
// Error codes of the company and billing pages (Slice 1E).
//
// RESPONSIBILITIES:
// - Name every refusal Platform can give these pages, in Operations terms
//
// IMPORTANT NOTES:
// - Codes, never text (L-1); Platform codes are translated in Infrastructure
// =========================================================
public static class OperationsCompanyErrors
{
    public static Error Refused { get; } = Error.Create("ops.company.refused");

    public static Error ChangePending { get; } = Error.Create("ops.company.change_pending");

    public static Error NoDifference { get; } = Error.Create("ops.company.no_difference");

    public static Error ProofRequired { get; } = Error.Create("ops.company.proof_required");

    public static Error ProofTooLarge { get; } = Error.Create("ops.company.proof_too_large");

    public static Error ProofTypeInvalid { get; } = Error.Create("ops.company.proof_type_invalid");

    public static Error ReasonRequired { get; } = Error.Create("ops.company.reason_required");

    public static Error NoChange { get; } = Error.Create("ops.company.no_change");

    public static Error DetailsInvalid { get; } = Error.Create("ops.company.details_invalid");

    public static Error ContactInvalid { get; } = Error.Create("ops.company.contact_invalid");

    public static Error TimeZoneInvalid { get; } = Error.Create("ops.company.time_zone_invalid");

    public static Error OwnerOrAdministrator { get; } = Error.Create("ops.billing.owner_or_administrator");

    public static Error InvoiceNotPayable { get; } = Error.Create("ops.billing.invoice_not_payable");

    public static Error BillingRefused { get; } = Error.Create("ops.billing.refused");
}
