namespace Vixly.Operations.Application.UseCases.Printing.ListOperationsLabelsToPrint;

// =========================================================
// FILE: ListOperationsLabelsToPrintRequest.cs
//
// PURPOSE:
// The cleared parcels whose shipping label was never printed, per warehouse, with the printers holding shipping labels (Slice 4D step 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsLabelsToPrintRequest
{
    public Guid? WarehouseId { get; init; }
}
