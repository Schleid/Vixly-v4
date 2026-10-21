namespace Vixly.Operations.Application.UseCases.Billing.GetMyOperationsCreditStatement;

// =========================================================
// FILE: GetMyOperationsCreditStatementRequest.cs
//
// PURPOSE:
// The customer's own credit statement as a PDF on the portal (Slice 5F step 2b, K-8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsCreditStatementRequest
{
    public string SessionToken { get; init; } = string.Empty;
}
