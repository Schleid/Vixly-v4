using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.DepartOperationsDispatchLeg;

// =========================================================
// FILE: DepartOperationsDispatchLegResponse.cs
//
// PURPOSE:
// Record that a leg left, at the time it did (Slice 6B, D-4 and D-5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DepartOperationsDispatchLegResponse
{
    private DepartOperationsDispatchLegResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DepartOperationsDispatchLegResponse Success()
    {
        return new DepartOperationsDispatchLegResponse { Succeeded = true };
    }

    public static DepartOperationsDispatchLegResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DepartOperationsDispatchLegResponse { Error = error };
    }
}
