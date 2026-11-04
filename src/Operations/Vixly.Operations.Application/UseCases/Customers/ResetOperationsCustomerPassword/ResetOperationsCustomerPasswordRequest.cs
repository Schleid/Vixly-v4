namespace Vixly.Operations.Application.UseCases.Customers.ResetOperationsCustomerPassword;

// =========================================================
// FILE: ResetOperationsCustomerPasswordRequest.cs
//
// PURPOSE:
// Sets a new password from a reset or invitation link, ends every session and signs in (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ResetOperationsCustomerPasswordRequest
{
    public string Token { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
