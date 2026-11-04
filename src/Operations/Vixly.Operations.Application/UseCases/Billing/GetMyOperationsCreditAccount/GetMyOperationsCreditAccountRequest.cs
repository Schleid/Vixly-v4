namespace Vixly.Operations.Application.UseCases.Billing.GetMyOperationsCreditAccount;

// =========================================================
// FILE: GetMyOperationsCreditAccountRequest.cs
//
// PURPOSE:
// The customer's own credit account on the portal (Slice 5F step 2b, K-8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsCreditAccountRequest
{
    public string SessionToken { get; init; } = string.Empty;
}
