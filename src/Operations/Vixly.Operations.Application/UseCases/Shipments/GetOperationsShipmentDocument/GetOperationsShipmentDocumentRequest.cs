using Vixly.Operations.Application.Shipments;

namespace Vixly.Operations.Application.UseCases.Shipments.GetOperationsShipmentDocument;

// =========================================================
// FILE: GetOperationsShipmentDocumentRequest.cs
//
// PURPOSE:
// The packing list or the pro forma commercial invoice of a shipment, for staff (Slice 5A).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsShipmentDocumentRequest
{
    public Guid ShipmentId { get; init; }

    public OperationsShipmentDocumentKind Kind { get; init; }
}
