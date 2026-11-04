using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.ScanIntoOperationsRepackBox;

// =========================================================
// FILE: ScanIntoOperationsRepackBoxResponse.cs
//
// PURPOSE:
// A worker scans a parcel of the shipment into its open repack box (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ScanIntoOperationsRepackBoxResponse
{
    private ScanIntoOperationsRepackBoxResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ParcelId { get; private init; }

    public bool Repeat { get; private init; }

    public static ScanIntoOperationsRepackBoxResponse Success(Guid parcelId, bool repeat)
    {
        return new ScanIntoOperationsRepackBoxResponse { Succeeded = true, ParcelId = parcelId, Repeat = repeat };
    }

    public static ScanIntoOperationsRepackBoxResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ScanIntoOperationsRepackBoxResponse { Error = error };
    }
}
