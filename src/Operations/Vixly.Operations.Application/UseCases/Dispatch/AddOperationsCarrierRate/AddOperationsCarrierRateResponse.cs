using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.AddOperationsCarrierRate;

// =========================================================
// FILE: AddOperationsCarrierRateResponse.cs
//
// PURPOSE:
// Add a carrier's rate to a destination from a day on (Slice 6B, D-1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AddOperationsCarrierRateResponse
{
    private AddOperationsCarrierRateResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid RateId { get; private init; }

    public static AddOperationsCarrierRateResponse Success(Guid rateId)
    {
        return new AddOperationsCarrierRateResponse { Succeeded = true, RateId = rateId };
    }

    public static AddOperationsCarrierRateResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AddOperationsCarrierRateResponse { Error = error };
    }
}
