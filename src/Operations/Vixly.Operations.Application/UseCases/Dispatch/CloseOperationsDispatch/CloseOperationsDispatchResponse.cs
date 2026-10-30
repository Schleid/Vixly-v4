using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.CloseOperationsDispatch;

// =========================================================
// FILE: CloseOperationsDispatchResponse.cs
//
// PURPOSE:
// Close (manifest) a dispatch with its bundles and legs (Slice 6B, D-2 and D-5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CloseOperationsDispatchResponse
{
    private CloseOperationsDispatchResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CloseOperationsDispatchResponse Success()
    {
        return new CloseOperationsDispatchResponse { Succeeded = true };
    }

    public static CloseOperationsDispatchResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CloseOperationsDispatchResponse { Error = error };
    }
}
