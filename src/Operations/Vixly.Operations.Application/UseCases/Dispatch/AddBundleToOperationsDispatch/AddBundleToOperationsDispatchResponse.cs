using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.AddBundleToOperationsDispatch;

// =========================================================
// FILE: AddBundleToOperationsDispatchResponse.cs
//
// PURPOSE:
// Put a closed bundle on a planned dispatch, chosen or scanned (Slice 6B, D-3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AddBundleToOperationsDispatchResponse
{
    private AddBundleToOperationsDispatchResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string BundleNumber { get; private init; } = string.Empty;

    public static AddBundleToOperationsDispatchResponse Success(string bundleNumber)
    {
        return new AddBundleToOperationsDispatchResponse { Succeeded = true, BundleNumber = bundleNumber };
    }

    public static AddBundleToOperationsDispatchResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AddBundleToOperationsDispatchResponse { Error = error };
    }
}
