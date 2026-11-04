using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.ChangeMyOperationsShipment;

// =========================================================
// FILE: ChangeMyOperationsShipmentResponse.cs
//
// PURPOSE:
// A customer changes where an open shipment goes, who receives it or its options (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeMyOperationsShipmentResponse
{
    private ChangeMyOperationsShipmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ShipmentId { get; private init; }

    public static ChangeMyOperationsShipmentResponse Success(Guid shipmentId)
    {
        return new ChangeMyOperationsShipmentResponse { Succeeded = true, ShipmentId = shipmentId };
    }

    public static ChangeMyOperationsShipmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeMyOperationsShipmentResponse { Error = error };
    }
}
