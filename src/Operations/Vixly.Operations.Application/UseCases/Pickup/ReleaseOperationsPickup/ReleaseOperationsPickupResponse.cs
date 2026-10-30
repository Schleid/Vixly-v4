using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Pickup.ReleaseOperationsPickup;

// =========================================================
// FILE: ReleaseOperationsPickupResponse.cs
//
// PURPOSE:
// Release a shipment's parcels ready at the pickup counter, once paid and with the proof the company asks (Slice 7 step 2, R-5, R-6, R-9).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReleaseOperationsPickupResponse
{
    private ReleaseOperationsPickupResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ReleaseId { get; private init; }

    public static ReleaseOperationsPickupResponse Success(Guid releaseId)
    {
        return new ReleaseOperationsPickupResponse { Succeeded = true, ReleaseId = releaseId };
    }

    public static ReleaseOperationsPickupResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReleaseOperationsPickupResponse { Error = error };
    }
}
