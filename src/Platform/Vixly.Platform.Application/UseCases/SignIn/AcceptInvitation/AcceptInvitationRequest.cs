namespace Vixly.Platform.Application.UseCases.SignIn.AcceptInvitation;

// =========================================================
// FILE: AcceptInvitationRequest.cs
//
// PURPOSE:
// Input for AcceptInvitation.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Anonymous endpoint, opened from the invitation email
// =========================================================
public sealed class AcceptInvitationRequest
{
    public string Token { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
