namespace Vixly.Operations.Application.UseCases.Printing.ListOperationsLabelPrinters;

// =========================================================
// FILE: ListOperationsLabelPrintersRequest.cs
//
// PURPOSE:
// The label printers of one warehouse, offered beside "Print all" (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Only printers that are on
// =========================================================
public sealed class ListOperationsLabelPrintersRequest
{
    public Guid WarehouseId { get; init; }
}
