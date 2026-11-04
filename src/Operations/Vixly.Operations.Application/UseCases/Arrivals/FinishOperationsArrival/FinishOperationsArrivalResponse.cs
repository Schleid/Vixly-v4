using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Arrivals.FinishOperationsArrival;

// =========================================================
// FILE: FinishOperationsArrivalResponse.cs
//
// PURPOSE:
// Finish receiving a dispatch: what did not come is listed as missing (Slice 7, R-2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class FinishOperationsArrivalResponse
{
    private FinishOperationsArrivalResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public int Missing { get; private init; }

    public static FinishOperationsArrivalResponse Success(int missing)
    {
        return new FinishOperationsArrivalResponse { Succeeded = true, Missing = missing };
    }

    public static FinishOperationsArrivalResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new FinishOperationsArrivalResponse { Error = error };
    }
}
