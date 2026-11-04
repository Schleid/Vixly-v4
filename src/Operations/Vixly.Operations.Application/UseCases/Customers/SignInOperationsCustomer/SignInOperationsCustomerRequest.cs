namespace Vixly.Operations.Application.UseCases.Customers.SignInOperationsCustomer;

// =========================================================
// FILE: SignInOperationsCustomerRequest.cs
//
// PURPOSE:
// A customer signs in with email and password (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SignInOperationsCustomerRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
