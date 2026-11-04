using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.CreateOperationsCounterShipment;

// =========================================================
// FILE: CreateOperationsCounterShipmentResponse.cs
//
// PURPOSE:
// Creates a customer's shipment at the counter, with its parcel count (Slice 5C, C-6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreateOperationsCounterShipmentResponse
{
    private CreateOperationsCounterShipmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ShipmentId { get; private init; }

    public static CreateOperationsCounterShipmentResponse Success(Guid shipmentId)
    {
        return new CreateOperationsCounterShipmentResponse { Succeeded = true, ShipmentId = shipmentId };
    }

    public static CreateOperationsCounterShipmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreateOperationsCounterShipmentResponse { Error = error };
    }
}
