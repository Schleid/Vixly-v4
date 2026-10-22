namespace Vixly.Operations.Application.UseCases.SignIn.ResetOperationsPassword;

// =========================================================
// FILE: ResetOperationsPasswordRequest.cs
//
// PURPOSE:
// Sets a new password from an emailed reset link (Slice 1C).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ResetOperationsPasswordRequest
{
    public string Token { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
