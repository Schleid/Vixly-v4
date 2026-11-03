namespace Vixly.Operations.Application.UseCases.Customers.GetMyOperationsShipmentDriver;

// =========================================================
// FILE: GetMyOperationsShipmentDriverRequest.cs
//
// PURPOSE:
// Where the driver bringing a customer's shipment is (Slice 12C, owner H-5): the stops before theirs, and the place once theirs is next.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsShipmentDriverRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid ShipmentId { get; init; }
}
