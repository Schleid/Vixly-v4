using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Holds.DecideOperationsUnclaimed;

// =========================================================
// FILE: DecideOperationsUnclaimedResponse.cs
//
// PURPOSE:
// The Owner or an administrator decides a package unclaimed past the company's days: return to sender or dispose of (Slice 3E, Q-R6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DecideOperationsUnclaimedResponse
{
    private DecideOperationsUnclaimedResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DecideOperationsUnclaimedResponse Success()
    {
        return new DecideOperationsUnclaimedResponse { Succeeded = true };
    }

    public static DecideOperationsUnclaimedResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DecideOperationsUnclaimedResponse { Error = error };
    }
}
