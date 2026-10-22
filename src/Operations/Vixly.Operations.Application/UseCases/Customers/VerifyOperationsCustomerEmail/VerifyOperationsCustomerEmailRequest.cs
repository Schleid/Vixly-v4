namespace Vixly.Operations.Application.UseCases.Customers.VerifyOperationsCustomerEmail;

// =========================================================
// FILE: VerifyOperationsCustomerEmailRequest.cs
//
// PURPOSE:
// Confirms a customer's email from the link: the account becomes active, gets its code and is signed in (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class VerifyOperationsCustomerEmailRequest
{
    public string Token { get; init; } = string.Empty;
}
