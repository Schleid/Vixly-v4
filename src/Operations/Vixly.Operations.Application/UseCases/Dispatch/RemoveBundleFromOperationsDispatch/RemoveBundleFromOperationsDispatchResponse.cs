using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.RemoveBundleFromOperationsDispatch;

// =========================================================
// FILE: RemoveBundleFromOperationsDispatchResponse.cs
//
// PURPOSE:
// Take a bundle off a planned dispatch (Slice 6B, D-3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemoveBundleFromOperationsDispatchResponse
{
    private RemoveBundleFromOperationsDispatchResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemoveBundleFromOperationsDispatchResponse Success()
    {
        return new RemoveBundleFromOperationsDispatchResponse { Succeeded = true };
    }

    public static RemoveBundleFromOperationsDispatchResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemoveBundleFromOperationsDispatchResponse { Error = error };
    }
}
