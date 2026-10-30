namespace Vixly.Operations.Application.UseCases.Printing.PrintOperationsShippingLabel;

// =========================================================
// FILE: PrintOperationsShippingLabelRequest.cs
//
// PURPOSE:
// Prints the shipping label of a cleared parcel on a printer holding 4 x 6 labels (Slice 4D step 2, A-5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PrintOperationsShippingLabelRequest
{
    public Guid ParcelId { get; init; }

    public Guid PrinterId { get; init; }

    // The labels to print, all at once: a parcel printed meanwhile (by another clerk) is skipped, never printed twice.
    public bool OnlyIfNotPrinted { get; init; }
}
