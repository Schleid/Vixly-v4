namespace Vixly.Operations.Application.UseCases.Customers.SignOutOperationsCustomer;

// =========================================================
// FILE: SignOutOperationsCustomerRequest.cs
//
// PURPOSE:
// Ends the customer's current session (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SignOutOperationsCustomerRequest
{
    public string SessionToken { get; init; } = string.Empty;
}
