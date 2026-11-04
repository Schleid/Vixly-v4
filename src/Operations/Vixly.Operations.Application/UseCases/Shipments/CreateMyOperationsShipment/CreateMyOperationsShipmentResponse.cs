using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.CreateMyOperationsShipment;

// =========================================================
// FILE: CreateMyOperationsShipmentResponse.cs
//
// PURPOSE:
// A customer creates a shipment: where it goes, who receives it, the options, and its first package (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreateMyOperationsShipmentResponse
{
    private CreateMyOperationsShipmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ShipmentId { get; private init; }

    public Guid? PreAlertId { get; private init; }

    public Guid? ParcelId { get; private init; }

    public static CreateMyOperationsShipmentResponse Success(Guid shipmentId, Guid? preAlertId, Guid? parcelId)
    {
        return new CreateMyOperationsShipmentResponse { Succeeded = true, ShipmentId = shipmentId, PreAlertId = preAlertId, ParcelId = parcelId };
    }

    public static CreateMyOperationsShipmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreateMyOperationsShipmentResponse { Error = error };
    }
}
