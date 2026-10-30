namespace Vixly.Operations.Application.UseCases.SignIn.RequestOperationsPasswordReset;

// =========================================================
// FILE: RequestOperationsPasswordResetRequest.cs
//
// PURPOSE:
// Emails a password reset link to a staff account (Slice 1C, "Forgot your password?").
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RequestOperationsPasswordResetRequest
{
    public string Email { get; init; } = string.Empty;

    public string PortalSlug { get; init; } = string.Empty;
}
