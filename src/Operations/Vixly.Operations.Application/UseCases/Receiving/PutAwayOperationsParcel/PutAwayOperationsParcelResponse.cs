using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.PutAwayOperationsParcel;

// =========================================================
// FILE: PutAwayOperationsParcelResponse.cs
//
// PURPOSE:
// Puts a parcel on a shelf of its warehouse (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PutAwayOperationsParcelResponse
{
    private PutAwayOperationsParcelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ParcelId { get; private init; }

    public static PutAwayOperationsParcelResponse Success(Guid parcelId)
    {
        return new PutAwayOperationsParcelResponse { Succeeded = true, ParcelId = parcelId };
    }

    public static PutAwayOperationsParcelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PutAwayOperationsParcelResponse { Error = error };
    }
}
