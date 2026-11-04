using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.CloseOperationsBundle;

// =========================================================
// FILE: CloseOperationsBundleResponse.cs
//
// PURPOSE:
// Close a bundle with its weight and size (Slice 6A, D-7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CloseOperationsBundleResponse
{
    private CloseOperationsBundleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CloseOperationsBundleResponse Success()
    {
        return new CloseOperationsBundleResponse { Succeeded = true };
    }

    public static CloseOperationsBundleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CloseOperationsBundleResponse { Error = error };
    }
}
