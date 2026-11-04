using Vixly.Operations.Domain.Printing;

namespace Vixly.Operations.Application.UseCases.Printing.SaveOperationsPrinter;

// =========================================================
// FILE: SaveOperationsPrinterRequest.cs
//
// PURPOSE:
// Adds a label printer to a warehouse, or changes one (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsPrinterRequest
{
    public Guid WarehouseId { get; init; }

    public Guid? PrinterId { get; init; }

    public string? Name { get; init; }

    public OperationsPrinterConnection Connection { get; init; } = OperationsPrinterConnection.Network;

    public string? Address { get; init; }

    public int Dpi { get; init; } = 203;

    // Slice 4D step 2: ID labels (dock) or shipping labels (inspection).
    public Vixly.Operations.Domain.Printing.OperationsLabelMedia Media { get; init; } = Vixly.Operations.Domain.Printing.OperationsLabelMedia.IdLabel;

    public bool Active { get; init; } = true;
}
