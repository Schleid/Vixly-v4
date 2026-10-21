namespace Vixly.Operations.Application.UseCases.Printing.PrintOperationsParcelLabel;

// =========================================================
// FILE: PrintOperationsParcelLabelRequest.cs
//
// PURPOSE:
// Prints a parcel's ID label again on a dock printer, or a test label (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PrintOperationsParcelLabelRequest
{
    public Guid PrinterId { get; init; }

    public Guid? ParcelId { get; init; }
}
