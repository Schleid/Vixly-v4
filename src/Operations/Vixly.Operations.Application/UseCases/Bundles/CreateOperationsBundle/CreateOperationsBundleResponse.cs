using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.CreateOperationsBundle;

// =========================================================
// FILE: CreateOperationsBundleResponse.cs
//
// PURPOSE:
// Open a new bundle for a destination and a service (Slice 6A, D-2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreateOperationsBundleResponse
{
    private CreateOperationsBundleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid BundleId { get; private init; }

    public static CreateOperationsBundleResponse Success(Guid bundleId)
    {
        return new CreateOperationsBundleResponse { Succeeded = true, BundleId = bundleId };
    }

    public static CreateOperationsBundleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreateOperationsBundleResponse { Error = error };
    }
}
