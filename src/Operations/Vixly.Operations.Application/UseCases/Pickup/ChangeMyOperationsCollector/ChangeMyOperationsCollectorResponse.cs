using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Pickup.ChangeMyOperationsCollector;

// =========================================================
// FILE: ChangeMyOperationsCollectorResponse.cs
//
// PURPOSE:
// The customer names who may collect a shipment, or no one (Slice 7 step 2, R-6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeMyOperationsCollectorResponse
{
    private ChangeMyOperationsCollectorResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeMyOperationsCollectorResponse Success()
    {
        return new ChangeMyOperationsCollectorResponse { Succeeded = true };
    }

    public static ChangeMyOperationsCollectorResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeMyOperationsCollectorResponse { Error = error };
    }
}
