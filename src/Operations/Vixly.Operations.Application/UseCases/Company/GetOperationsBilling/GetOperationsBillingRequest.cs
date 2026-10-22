namespace Vixly.Operations.Application.UseCases.Company.GetOperationsBilling;

// =========================================================
// FILE: GetOperationsBillingRequest.cs
//
// PURPOSE:
// Reads the plan, invoices and billing PDFs for the billing page (Slice 1E).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsBillingRequest
{
    public string Language { get; init; } = "en";

    public int Page { get; init; } = 1;
}
