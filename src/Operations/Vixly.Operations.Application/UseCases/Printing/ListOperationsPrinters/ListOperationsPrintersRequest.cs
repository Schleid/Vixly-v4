namespace Vixly.Operations.Application.UseCases.Printing.ListOperationsPrinters;

// =========================================================
// FILE: ListOperationsPrintersRequest.cs
//
// PURPOSE:
// The label printers and dock print programs of a warehouse (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsPrintersRequest
{
    public Guid WarehouseId { get; init; }
}
