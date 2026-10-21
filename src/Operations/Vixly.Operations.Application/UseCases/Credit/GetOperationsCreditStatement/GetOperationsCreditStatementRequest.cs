namespace Vixly.Operations.Application.UseCases.Credit.GetOperationsCreditStatement;

// =========================================================
// FILE: GetOperationsCreditStatementRequest.cs
//
// PURPOSE:
// A credit account's statement as a PDF (Slice 5F step 2b, K-7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsCreditStatementRequest
{
    public Guid CustomerId { get; init; }
}
