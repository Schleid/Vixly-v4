namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsShipmentDocumentKind.cs
//
// PURPOSE:
// The two documents of a shipment (Slice 5A).
//
// RESPONSIBILITIES:
// - Name the packing list and the pro forma commercial invoice
//
// IMPORTANT NOTES:
// - Used in addresses as packing-list and commercial-invoice
// =========================================================
public enum OperationsShipmentDocumentKind
{
    PackingList = 1,

    CommercialInvoice = 2
}
