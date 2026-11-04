using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Insurance;

// =========================================================
// FILE: OperationsInsuranceErrors.cs
//
// PURPOSE:
// Error codes of the insurance module (Slice 5B).
//
// RESPONSIBILITIES:
// - One code per refusal
//
// IMPORTANT NOTES:
// - Texts in OperationsText
// =========================================================
public static class OperationsInsuranceErrors
{
    public static Error NotFound { get; } = Error.Create("ops.insurance.not_found");

    public static Error DetailsInvalid { get; } = Error.Create("ops.insurance.details_invalid");

    public static Error KindInvalid { get; } = Error.Create("ops.insurance.kind_invalid");

    public static Error DatesInvalid { get; } = Error.Create("ops.insurance.dates_invalid");

    public static Error AmountInvalid { get; } = Error.Create("ops.insurance.amount_invalid");

    public static Error DocumentInvalid { get; } = Error.Create("ops.insurance.document_invalid");

    public static Error TooManyDocuments { get; } = Error.Create("ops.insurance.too_many_documents");

    public static Error LiabilityInvalid { get; } = Error.Create("ops.insurance.liability_invalid");

    // A cover sold to customers sits under a customer-shipments policy only.
    public static Error PolicyScopeInvalid { get; } = Error.Create("ops.insurance.policy_scope_invalid");

    // A cover sits under a policy that covers its transport mode (review).
    public static Error PolicyModeInvalid { get; } = Error.Create("ops.insurance.policy_mode_invalid");
}
