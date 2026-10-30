using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.RemoveOperationsDispatchLeg;

// =========================================================
// FILE: RemoveOperationsDispatchLegResponse.cs
//
// PURPOSE:
// Remove a leg of a dispatch before it leaves (Slice 6B, D-4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemoveOperationsDispatchLegResponse
{
    private RemoveOperationsDispatchLegResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemoveOperationsDispatchLegResponse Success()
    {
        return new RemoveOperationsDispatchLegResponse { Succeeded = true };
    }

    public static RemoveOperationsDispatchLegResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemoveOperationsDispatchLegResponse { Error = error };
    }
}
