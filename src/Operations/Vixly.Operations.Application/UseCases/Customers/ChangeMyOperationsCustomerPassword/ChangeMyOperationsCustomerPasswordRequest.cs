namespace Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsCustomerPassword;

// =========================================================
// FILE: ChangeMyOperationsCustomerPasswordRequest.cs
//
// PURPOSE:
// The signed-in customer changes their password; their other sessions end (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeMyOperationsCustomerPasswordRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
