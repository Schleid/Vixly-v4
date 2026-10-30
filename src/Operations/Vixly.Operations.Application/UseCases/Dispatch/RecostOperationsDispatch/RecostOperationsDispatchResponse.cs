using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.RecostOperationsDispatch;

// =========================================================
// FILE: RecostOperationsDispatchResponse.cs
//
// PURPOSE:
// Work out a dispatch's carrier cost again from the rates in force (Slice 6B step 2, D-7): after a rate was added, or a leg's carrier changed.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RecostOperationsDispatchResponse
{
    private RecostOperationsDispatchResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<int> LegsWithoutRate { get; private init; } = Array.Empty<int>();

    public static RecostOperationsDispatchResponse Success(IReadOnlyList<int> legsWithoutRate)
    {
        return new RecostOperationsDispatchResponse { Succeeded = true, LegsWithoutRate = legsWithoutRate };
    }

    public static RecostOperationsDispatchResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RecostOperationsDispatchResponse { Error = error };
    }
}
