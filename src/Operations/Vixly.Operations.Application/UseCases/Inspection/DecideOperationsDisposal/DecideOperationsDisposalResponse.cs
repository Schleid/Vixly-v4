using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.DecideOperationsDisposal;

// =========================================================
// FILE: DecideOperationsDisposalResponse.cs
//
// PURPOSE:
// The Owner or an administrator approves or declines disposing of a refused parcel (Slice 3C, Q-C3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DecideOperationsDisposalResponse
{
    private DecideOperationsDisposalResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DecideOperationsDisposalResponse Success()
    {
        return new DecideOperationsDisposalResponse { Succeeded = true };
    }

    public static DecideOperationsDisposalResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DecideOperationsDisposalResponse { Error = error };
    }
}
