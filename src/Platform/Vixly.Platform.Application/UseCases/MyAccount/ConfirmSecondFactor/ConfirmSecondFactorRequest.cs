namespace Vixly.Platform.Application.UseCases.MyAccount.ConfirmSecondFactor;

// =========================================================
// FILE: ConfirmSecondFactorRequest.cs
//
// PURPOSE:
// Input for ConfirmSecondFactor.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The session comes from the context, never the request
// - UseRecoveryCode spends one of the user's recovery codes
// =========================================================
public sealed class ConfirmSecondFactorRequest
{
    public string Code { get; init; } = string.Empty;

    public bool UseRecoveryCode { get; init; }
}
