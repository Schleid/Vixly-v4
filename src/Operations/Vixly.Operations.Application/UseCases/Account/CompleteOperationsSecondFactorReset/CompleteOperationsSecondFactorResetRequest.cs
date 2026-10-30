namespace Vixly.Operations.Application.UseCases.Account.CompleteOperationsSecondFactorReset;

// =========================================================
// FILE: CompleteOperationsSecondFactorResetRequest.cs
//
// PURPOSE:
// Completes the second-factor reset Vixly support sent to the Owner (D-O13, Slice 1C).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CompleteOperationsSecondFactorResetRequest
{
    public string Token { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
