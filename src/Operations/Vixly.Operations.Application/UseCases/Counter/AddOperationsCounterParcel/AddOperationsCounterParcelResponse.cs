using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.AddOperationsCounterParcel;

// =========================================================
// FILE: AddOperationsCounterParcelResponse.cs
//
// PURPOSE:
// Enters a parcel at the counter: weighed, measured and described in front of the customer; the last one makes the shipment final (Slice 5C, C-2 and C-7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AddOperationsCounterParcelResponse
{
    private AddOperationsCounterParcelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ParcelId { get; private init; }

    public bool Booked { get; private init; }

    public static AddOperationsCounterParcelResponse Success(Guid parcelId, bool booked)
    {
        return new AddOperationsCounterParcelResponse { Succeeded = true, ParcelId = parcelId, Booked = booked };
    }

    public static AddOperationsCounterParcelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AddOperationsCounterParcelResponse { Error = error };
    }
}
