using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.CreateOperationsDispatch;

// =========================================================
// FILE: CreateOperationsDispatchResponse.cs
//
// PURPOSE:
// Plan a dispatch from a warehouse to a destination by a service (Slice 6B, D-2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreateOperationsDispatchResponse
{
    private CreateOperationsDispatchResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid DispatchId { get; private init; }

    public static CreateOperationsDispatchResponse Success(Guid dispatchId)
    {
        return new CreateOperationsDispatchResponse { Succeeded = true, DispatchId = dispatchId };
    }

    public static CreateOperationsDispatchResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreateOperationsDispatchResponse { Error = error };
    }
}
