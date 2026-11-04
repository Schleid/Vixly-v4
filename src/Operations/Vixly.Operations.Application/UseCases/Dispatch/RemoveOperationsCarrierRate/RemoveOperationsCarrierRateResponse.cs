using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.RemoveOperationsCarrierRate;

// =========================================================
// FILE: RemoveOperationsCarrierRateResponse.cs
//
// PURPOSE:
// Remove a rate that has not started yet (Slice 6B, D-1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemoveOperationsCarrierRateResponse
{
    private RemoveOperationsCarrierRateResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemoveOperationsCarrierRateResponse Success()
    {
        return new RemoveOperationsCarrierRateResponse { Succeeded = true };
    }

    public static RemoveOperationsCarrierRateResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemoveOperationsCarrierRateResponse { Error = error };
    }
}
