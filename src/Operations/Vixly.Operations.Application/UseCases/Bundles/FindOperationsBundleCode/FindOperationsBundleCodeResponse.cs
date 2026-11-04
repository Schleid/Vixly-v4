using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.FindOperationsBundleCode;

// =========================================================
// FILE: FindOperationsBundleCodeResponse.cs
//
// PURPOSE:
// The fill station's first scan (Slice 6A, D-3): the bundle or the repack box whose barcode was scanned.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class FindOperationsBundleCodeResponse
{
    private FindOperationsBundleCodeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid? BundleId { get; private init; }

    public Guid? BoxId { get; private init; }

    public static FindOperationsBundleCodeResponse Success(Guid? bundleId, Guid? boxId)
    {
        return new FindOperationsBundleCodeResponse { Succeeded = true, BundleId = bundleId, BoxId = boxId };
    }

    public static FindOperationsBundleCodeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new FindOperationsBundleCodeResponse { Error = error };
    }
}
