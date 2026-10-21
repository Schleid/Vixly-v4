using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Account.ConfirmOperationsSecondFactor;

// =========================================================
// FILE: ConfirmOperationsSecondFactorResponse.cs
//
// PURPOSE:
// Confirms my second factor for the next 5 minutes of sensitive actions (step-up, Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ConfirmOperationsSecondFactorResponse
{
    private ConfirmOperationsSecondFactorResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ConfirmOperationsSecondFactorResponse Success()
    {
        return new ConfirmOperationsSecondFactorResponse { Succeeded = true };
    }

    public static ConfirmOperationsSecondFactorResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ConfirmOperationsSecondFactorResponse { Error = error };
    }
}
