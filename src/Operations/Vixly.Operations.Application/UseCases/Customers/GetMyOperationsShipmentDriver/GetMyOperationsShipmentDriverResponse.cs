using Vixly.Operations.Application.Delivery;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.GetMyOperationsShipmentDriver;

// =========================================================
// FILE: GetMyOperationsShipmentDriverResponse.cs
//
// PURPOSE:
// Where the driver bringing a customer's shipment is (Slice 12C, owner H-5): the stops before theirs, and the place once theirs is next.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsShipmentDriverResponse
{
    private GetMyOperationsShipmentDriverResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsDriverWhereabouts? Driver { get; private init; }

    public static GetMyOperationsShipmentDriverResponse Success(OperationsDriverWhereabouts? driver)
    {
        return new GetMyOperationsShipmentDriverResponse { Succeeded = true, Driver = driver };
    }

    public static GetMyOperationsShipmentDriverResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsShipmentDriverResponse { Error = error };
    }
}
