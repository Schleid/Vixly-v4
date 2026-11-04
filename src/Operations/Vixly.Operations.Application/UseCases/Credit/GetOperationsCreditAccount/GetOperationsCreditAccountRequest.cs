namespace Vixly.Operations.Application.UseCases.Credit.GetOperationsCreditAccount;

// =========================================================
// FILE: GetOperationsCreditAccountRequest.cs
//
// PURPOSE:
// A customer's credit account (Slice 5F step 2b, K-6): its terms, its open items with their age, and what can be changed.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsCreditAccountRequest
{
    public Guid CustomerId { get; init; }
}
