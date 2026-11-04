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

    // What was moved where, for the move station to confirm.
    public string? ParcelNumber { get; private init; }

    public string? ShelfCode { get; private init; }

    public static PutAwayOperationsParcelResponse Success(Guid parcelId, string? parcelNumber = null, string? shelfCode = null)
    {
        return new PutAwayOperationsParcelResponse { Succeeded = true, ParcelId = parcelId, ParcelNumber = parcelNumber, ShelfCode = shelfCode };
    }

    public static PutAwayOperationsParcelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PutAwayOperationsParcelResponse { Error = error };
    }
}
