using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Printing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.ListOperationsLabelsToPrint;

// =========================================================
// FILE: ListOperationsLabelsToPrintResponse.cs
//
// PURPOSE:
// The cleared parcels whose shipping label was never printed, per warehouse, with the printers holding shipping labels (Slice 4D step 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsLabelsToPrintResponse
{
    private ListOperationsLabelsToPrintResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsParcelSummary> Parcels { get; private init; } = Array.Empty<OperationsParcelSummary>();

    public IReadOnlyList<OperationsLabelPrinter> Printers { get; private init; } = Array.Empty<OperationsLabelPrinter>();

    public IReadOnlyList<Vixly.Operations.Domain.Locations.OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<Vixly.Operations.Domain.Locations.OperationsWarehouse>();

    public static ListOperationsLabelsToPrintResponse Success(IReadOnlyList<OperationsParcelSummary> parcels, IReadOnlyList<OperationsLabelPrinter> printers, IReadOnlyList<Vixly.Operations.Domain.Locations.OperationsWarehouse> warehouses)
    {
        return new ListOperationsLabelsToPrintResponse { Succeeded = true, Parcels = parcels, Printers = printers, Warehouses = warehouses };
    }

    public static ListOperationsLabelsToPrintResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsLabelsToPrintResponse { Error = error };
    }
}
