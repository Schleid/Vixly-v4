using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Account.CompleteOperationsSecondFactorReset;

// =========================================================
// FILE: CompleteOperationsSecondFactorResetResponse.cs
//
// PURPOSE:
// Completes the second-factor reset Vixly support sent to the Owner (D-O13, Slice 1C).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CompleteOperationsSecondFactorResetResponse
{
    private CompleteOperationsSecondFactorResetResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string ChallengeToken { get; private init; } = string.Empty;

    public static CompleteOperationsSecondFactorResetResponse Success(string challengeToken)
    {
        return new CompleteOperationsSecondFactorResetResponse { Succeeded = true, ChallengeToken = challengeToken };
    }

    public static CompleteOperationsSecondFactorResetResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompleteOperationsSecondFactorResetResponse { Error = error };
    }
}
