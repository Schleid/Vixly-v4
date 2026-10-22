using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.FindOperationsParcelByScan;

// =========================================================
// FILE: FindOperationsParcelByScanResponse.cs
//
// PURPOSE:
// Finds a parcel from a scan at a station: its ID label, or the carrier barcode (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class FindOperationsParcelByScanResponse
{
    private FindOperationsParcelByScanResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ParcelId { get; private init; }

    public static FindOperationsParcelByScanResponse Success(Guid parcelId)
    {
        return new FindOperationsParcelByScanResponse { Succeeded = true, ParcelId = parcelId };
    }

    public static FindOperationsParcelByScanResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new FindOperationsParcelByScanResponse { Error = error };
    }
}
