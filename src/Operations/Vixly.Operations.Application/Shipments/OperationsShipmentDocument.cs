namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsShipmentDocument.cs
//
// PURPOSE:
// The data of a packing list or a pro forma commercial invoice (Slice 5A).
//
// RESPONSIBILITIES:
// - Carry the company, the shipment, the shipper, the consignee, the lines and the totals
//
// IMPORTANT NOTES:
// - Rendered by IOperationsLabelRenderer; never "Vixly", only the company's name
// =========================================================
public sealed record OperationsShipmentDocument(
    OperationsShipmentDocumentKind Kind,
    string CompanyName,
    string ShipmentNumber,
    DateOnly IssuedOn,
    string Origin,
    string Destination,
    string? ServiceCode,
    string? ServiceLabel,
    IReadOnlyList<string> Shipper,
    IReadOnlyList<string> Consignee,
    IReadOnlyList<OperationsShipmentDocumentLine> Lines,
    string TotalWeight,
    decimal TotalValue,
    string CurrencyCode);
