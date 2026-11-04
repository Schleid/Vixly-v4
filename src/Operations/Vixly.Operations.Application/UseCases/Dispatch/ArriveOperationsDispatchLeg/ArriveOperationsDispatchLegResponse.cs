using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.ArriveOperationsDispatchLeg;

// =========================================================
// FILE: ArriveOperationsDispatchLegResponse.cs
//
// PURPOSE:
// Record that a leg arrived, at the time it did (Slice 6B, D-4 and D-5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ArriveOperationsDispatchLegResponse
{
    private ArriveOperationsDispatchLegResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ArriveOperationsDispatchLegResponse Success()
    {
        return new ArriveOperationsDispatchLegResponse { Succeeded = true };
    }

    public static ArriveOperationsDispatchLegResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ArriveOperationsDispatchLegResponse { Error = error };
    }
}
