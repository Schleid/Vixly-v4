namespace Vixly.Operations.Application.UseCases.Account.ConfirmOperationsSecondFactor;

// =========================================================
// FILE: ConfirmOperationsSecondFactorRequest.cs
//
// PURPOSE:
// Confirms my second factor for the next 5 minutes of sensitive actions (step-up, Slice 1D).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ConfirmOperationsSecondFactorRequest
{
    public string Code { get; init; } = string.Empty;
}
