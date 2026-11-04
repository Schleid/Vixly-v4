namespace Vixly.Operations.Application.UseCases.Printing.PrintOperationsShipmentLabels;

// =========================================================
// FILE: PrintOperationsShipmentLabelsRequest.cs
//
// PURPOSE:
// Print all the labels of one shipment at once (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Shipping false: the parcels' 2 x 1 ID labels; true: their 4 x 6 shipping labels
// - PrinterId null: one PDF for the browser; otherwise sent to that label printer
// =========================================================
public sealed class PrintOperationsShipmentLabelsRequest
{
    public Guid ShipmentId { get; init; }

    public bool Shipping { get; init; }

    public Guid? PrinterId { get; init; }
}
