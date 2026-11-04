using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.AddOperationsDispatchLeg;

// =========================================================
// FILE: AddOperationsDispatchLegResponse.cs
//
// PURPOSE:
// Add a leg to a dispatch before it leaves (Slice 6B, D-4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AddOperationsDispatchLegResponse
{
    private AddOperationsDispatchLegResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid LegId { get; private init; }

    public static AddOperationsDispatchLegResponse Success(Guid legId)
    {
        return new AddOperationsDispatchLegResponse { Succeeded = true, LegId = legId };
    }

    public static AddOperationsDispatchLegResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AddOperationsDispatchLegResponse { Error = error };
    }
}
