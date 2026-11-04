using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.VoidOperationsBundle;

// =========================================================
// FILE: VoidOperationsBundleResponse.cs
//
// PURPOSE:
// Void a bundle with a reason (Slice 6A, D-8; owner, A-4): its parcels and boxes are ready to bundle again.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class VoidOperationsBundleResponse
{
    private VoidOperationsBundleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static VoidOperationsBundleResponse Success()
    {
        return new VoidOperationsBundleResponse { Succeeded = true };
    }

    public static VoidOperationsBundleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new VoidOperationsBundleResponse { Error = error };
    }
}
