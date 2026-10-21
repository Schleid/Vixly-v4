using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.CancelMyOperationsShipment;

// =========================================================
// FILE: CancelMyOperationsShipmentResponse.cs
//
// PURPOSE:
// A customer cancels a shipment none of whose packages has arrived (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CancelMyOperationsShipmentResponse
{
    private CancelMyOperationsShipmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ShipmentId { get; private init; }

    public static CancelMyOperationsShipmentResponse Success(Guid shipmentId)
    {
        return new CancelMyOperationsShipmentResponse { Succeeded = true, ShipmentId = shipmentId };
    }

    public static CancelMyOperationsShipmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CancelMyOperationsShipmentResponse { Error = error };
    }
}
