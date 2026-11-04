namespace Vixly.Operations.Application.UseCases.Account.ChangeMyOperationsPassword;

// =========================================================
// FILE: ChangeMyOperationsPasswordRequest.cs
//
// PURPOSE:
// Changes my password; every other session of mine ends (Slice 1D).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeMyOperationsPasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
