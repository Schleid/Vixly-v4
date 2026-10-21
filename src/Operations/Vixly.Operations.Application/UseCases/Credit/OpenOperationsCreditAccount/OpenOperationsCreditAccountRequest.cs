namespace Vixly.Operations.Application.UseCases.Credit.OpenOperationsCreditAccount;

// =========================================================
// FILE: OpenOperationsCreditAccountRequest.cs
//
// PURPOSE:
// Open a credit account for a customer (Slice 5F step 2b, K-1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class OpenOperationsCreditAccountRequest
{
    // The customer by id, or by code typed by the credit department.
    public Guid? CustomerId { get; init; }

    public string? CustomerCode { get; init; }

    public decimal Limit { get; init; }

    // None: the company's terms.
    public int? TermsDays { get; init; }
}
