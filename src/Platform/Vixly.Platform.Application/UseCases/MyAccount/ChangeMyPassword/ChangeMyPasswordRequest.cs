namespace Vixly.Platform.Application.UseCases.MyAccount.ChangeMyPassword;

// =========================================================
// FILE: ChangeMyPasswordRequest.cs
//
// PURPOSE:
// Input for ChangeMyPassword.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Never logged or audited
// =========================================================
public sealed class ChangeMyPasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
