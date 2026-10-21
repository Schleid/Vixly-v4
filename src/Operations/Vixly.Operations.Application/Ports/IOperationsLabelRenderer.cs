namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsLabelRenderer.cs
//
// PURPOSE:
// Draws labels as PDF (Slice 3B).
//
// RESPONSIBILITIES:
// - Render the 4 x 6 inch parcel ID label with a Code 128 barcode
//
// IMPORTANT NOTES:
// - Thermal printers print the PDF at its size
// =========================================================
public interface IOperationsLabelRenderer
{
    // The 2 x 1 inch parcel ID label as a PDF, for a station that prints from the browser.
    byte[] RenderParcelLabel(OperationsParcelLabel label);

    // Slice 4D step 2: the 4 x 6 inch shipping label as a PDF, for a station that prints from the browser.
    byte[] RenderShippingLabel(OperationsShippingLabel label);

    // The warehouse receipt of a carrier delivery, for the driver to sign.
    byte[] RenderReceipt(OperationsReceiptDocument receipt);

    // Slice 5A: the packing list or the pro forma commercial invoice of a shipment.
    byte[] RenderShipmentDocument(Vixly.Operations.Application.Shipments.OperationsShipmentDocument document);

    // Slice 5D: the page the customer signs at the counter.
    byte[] RenderCustomerDeclaration(Vixly.Operations.Application.Shipments.OperationsCustomerDeclaration declaration);

    // Slice 5B: a shipment's insurance certificate.
    byte[] RenderInsuranceCertificate(Vixly.Operations.Application.Insurance.OperationsCertificateDocument certificate);

    // Slice 5E: a Letter sheet of shelf labels for a supply (3 by 10), each with its name, its code as a barcode and its price.
    // Slice 5F: an invoice or one of its credit notes, on Letter paper.
    byte[] RenderInvoice(Vixly.Operations.Application.Billing.OperationsInvoiceDocument document);

    byte[] RenderSupplyLabels(Vixly.Operations.Application.Supplies.OperationsSupplyLabel label, int count);
}
