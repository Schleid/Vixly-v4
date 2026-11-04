using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.CorrectOperationsDispatchCost;

// =========================================================
// FILE: CorrectOperationsDispatchCostResponse.cs
//
// PURPOSE:
// The dispatcher's cost for a dispatch, with a note (Slice 6B step 2, D-7): the carrier's invoice differs, or no rate was set.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CorrectOperationsDispatchCostResponse
{
    private CorrectOperationsDispatchCostResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CorrectOperationsDispatchCostResponse Success()
    {
        return new CorrectOperationsDispatchCostResponse { Succeeded = true };
    }

    public static CorrectOperationsDispatchCostResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CorrectOperationsDispatchCostResponse { Error = error };
    }
}
