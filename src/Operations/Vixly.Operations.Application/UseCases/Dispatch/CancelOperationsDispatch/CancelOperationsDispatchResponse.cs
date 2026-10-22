using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.CancelOperationsDispatch;

// =========================================================
// FILE: CancelOperationsDispatchResponse.cs
//
// PURPOSE:
// Cancel a dispatch that has not left, with a reason (Slice 6B, D-2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CancelOperationsDispatchResponse
{
    private CancelOperationsDispatchResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CancelOperationsDispatchResponse Success()
    {
        return new CancelOperationsDispatchResponse { Succeeded = true };
    }

    public static CancelOperationsDispatchResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CancelOperationsDispatchResponse { Error = error };
    }
}
