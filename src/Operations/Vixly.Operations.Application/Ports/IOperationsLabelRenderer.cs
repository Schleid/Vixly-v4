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

    // Slice 5F step 2: the receipt of a payment.
    byte[] RenderPaymentReceipt(Vixly.Operations.Application.Billing.OperationsPaymentReceipt receipt);

    // Slice 6A: the 4 x 6 labels of a bundle and of a repack box.
    byte[] RenderBundleLabel(Vixly.Operations.Application.Bundles.OperationsBundleLabel label);

    byte[] RenderRepackBoxLabel(Vixly.Operations.Application.Bundles.OperationsRepackBoxLabel label);

    // Slice 5F step 2b: a credit account's statement, on Letter paper.
    byte[] RenderCreditStatement(Vixly.Operations.Application.Billing.OperationsCreditStatement statement);

    // Slice 6B: the dispatch manifest and a bundle's manifest, on Letter paper.
    byte[] RenderDispatchManifest(Vixly.Operations.Application.Dispatch.OperationsDispatchManifest manifest);

    byte[] RenderBundleManifest(Vixly.Operations.Application.Dispatch.OperationsBundleManifest manifest);

    // Slice 6B step 2: house air waybills (one page each) and the Shipper's Letter of Instruction, on Letter paper.
    byte[] RenderHouseWaybills(IReadOnlyList<Vixly.Operations.Application.Dispatch.OperationsHouseWaybill> waybills);

    byte[] RenderShipperLetter(Vixly.Operations.Application.Dispatch.OperationsShipperLetter letter);

    byte[] RenderSupplyLabels(Vixly.Operations.Application.Supplies.OperationsSupplyLabel label, int count);
}
