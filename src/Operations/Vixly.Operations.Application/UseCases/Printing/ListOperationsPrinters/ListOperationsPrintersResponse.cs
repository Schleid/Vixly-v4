using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.ListOperationsPrinters;

// =========================================================
// FILE: ListOperationsPrintersResponse.cs
//
// PURPOSE:
// The label printers and dock print programs of a warehouse (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsPrintersResponse
{
    private ListOperationsPrintersResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsWarehouse? Warehouse { get; private init; }

    public IReadOnlyList<OperationsLabelPrinter> Printers { get; private init; } = Array.Empty<OperationsLabelPrinter>();

    public IReadOnlyList<OperationsPrintAgent> Agents { get; private init; } = Array.Empty<OperationsPrintAgent>();

    public static ListOperationsPrintersResponse Success(OperationsWarehouse? warehouse, IReadOnlyList<OperationsLabelPrinter> printers, IReadOnlyList<OperationsPrintAgent> agents)
    {
        return new ListOperationsPrintersResponse { Succeeded = true, Warehouse = warehouse, Printers = printers, Agents = agents };
    }

    public static ListOperationsPrintersResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsPrintersResponse { Error = error };
    }
}
