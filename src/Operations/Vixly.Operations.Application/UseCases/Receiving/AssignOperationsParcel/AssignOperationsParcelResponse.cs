using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.AssignOperationsParcel;

// =========================================================
// FILE: AssignOperationsParcelResponse.cs
//
// PURPOSE:
// Gives an unidentified parcel to the customer who proved it is theirs (Slice 3B, owner Q-R1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AssignOperationsParcelResponse
{
    private AssignOperationsParcelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ParcelId { get; private init; }

    public static AssignOperationsParcelResponse Success(Guid parcelId)
    {
        return new AssignOperationsParcelResponse { Succeeded = true, ParcelId = parcelId };
    }

    public static AssignOperationsParcelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AssignOperationsParcelResponse { Error = error };
    }
}
