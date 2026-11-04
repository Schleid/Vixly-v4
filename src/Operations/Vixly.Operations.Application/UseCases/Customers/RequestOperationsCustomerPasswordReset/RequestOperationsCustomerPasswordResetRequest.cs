namespace Vixly.Operations.Application.UseCases.Customers.RequestOperationsCustomerPasswordReset;

// =========================================================
// FILE: RequestOperationsCustomerPasswordResetRequest.cs
//
// PURPOSE:
// Emails a password-reset link; the same answer for unknown emails (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RequestOperationsCustomerPasswordResetRequest
{
    public string Email { get; init; } = string.Empty;

    public string PortalSlug { get; init; } = string.Empty;
}
