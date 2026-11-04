using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.ChangeOperationsShipmentService;

// =========================================================
// FILE: ChangeOperationsShipmentServiceResponse.cs
//
// PURPOSE:
// Staff change the service of a shipment before it leaves (Slice 5A; owner, Q-D1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsShipmentServiceResponse
{
    private ChangeOperationsShipmentServiceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsShipmentServiceResponse Success()
    {
        return new ChangeOperationsShipmentServiceResponse { Succeeded = true };
    }

    public static ChangeOperationsShipmentServiceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsShipmentServiceResponse { Error = error };
    }
}
