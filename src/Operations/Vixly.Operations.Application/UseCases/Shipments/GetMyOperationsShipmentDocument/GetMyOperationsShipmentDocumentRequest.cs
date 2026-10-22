using Vixly.Operations.Application.Shipments;

namespace Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipmentDocument;

// =========================================================
// FILE: GetMyOperationsShipmentDocumentRequest.cs
//
// PURPOSE:
// The packing list or the pro forma commercial invoice of a shipment, for the customer (Slice 5A).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsShipmentDocumentRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid ShipmentId { get; init; }

    public OperationsShipmentDocumentKind Kind { get; init; }
}
