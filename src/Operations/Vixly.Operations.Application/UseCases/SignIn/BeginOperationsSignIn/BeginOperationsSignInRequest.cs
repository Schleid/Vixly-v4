namespace Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignIn;

// =========================================================
// FILE: BeginOperationsSignInRequest.cs
//
// PURPOSE:
// Checks the email and password of a staff member (Slice 1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class BeginOperationsSignInRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
