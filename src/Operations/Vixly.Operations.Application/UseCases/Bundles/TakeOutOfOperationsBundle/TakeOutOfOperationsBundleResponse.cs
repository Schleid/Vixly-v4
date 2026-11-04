using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.TakeOutOfOperationsBundle;

// =========================================================
// FILE: TakeOutOfOperationsBundleResponse.cs
//
// PURPOSE:
// Take a parcel or a repack box out of an open bundle: a wrong scan (Slice 6A, D-6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class TakeOutOfOperationsBundleResponse
{
    private TakeOutOfOperationsBundleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static TakeOutOfOperationsBundleResponse Success()
    {
        return new TakeOutOfOperationsBundleResponse { Succeeded = true };
    }

    public static TakeOutOfOperationsBundleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new TakeOutOfOperationsBundleResponse { Error = error };
    }
}
