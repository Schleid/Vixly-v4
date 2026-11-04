using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.ScanIntoOperationsBundle;

// =========================================================
// FILE: ScanIntoOperationsBundleResponse.cs
//
// PURPOSE:
// A worker scans a parcel or a repack box into an open bundle (Slice 6A, D-3, D-4 and D-5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ScanIntoOperationsBundleResponse
{
    private ScanIntoOperationsBundleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid? ParcelId { get; private init; }

    public Guid? BoxId { get; private init; }

    public bool Repeat { get; private init; }

    public bool PastTarget { get; private init; }

    public bool Split { get; private init; }

    public static ScanIntoOperationsBundleResponse Success(Guid? parcelId, Guid? boxId, bool repeat, bool pastTarget, bool split)
    {
        return new ScanIntoOperationsBundleResponse { Succeeded = true, ParcelId = parcelId, BoxId = boxId, Repeat = repeat, PastTarget = pastTarget, Split = split };
    }

    public static ScanIntoOperationsBundleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ScanIntoOperationsBundleResponse { Error = error };
    }
}
