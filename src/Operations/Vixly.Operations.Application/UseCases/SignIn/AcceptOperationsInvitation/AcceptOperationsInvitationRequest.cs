namespace Vixly.Operations.Application.UseCases.SignIn.AcceptOperationsInvitation;

// =========================================================
// FILE: AcceptOperationsInvitationRequest.cs
//
// PURPOSE:
// Sets the password from an invitation link and continues to the second factor (Slice 1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AcceptOperationsInvitationRequest
{
    public string Token { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
