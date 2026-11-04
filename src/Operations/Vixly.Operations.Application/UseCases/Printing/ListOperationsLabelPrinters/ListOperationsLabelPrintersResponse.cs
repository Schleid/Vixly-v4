using Vixly.Operations.Domain.Printing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.ListOperationsLabelPrinters;

// =========================================================
// FILE: ListOperationsLabelPrintersResponse.cs
//
// PURPOSE:
// The warehouse's label printers that are on (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Carry the printers; none when the reader may not print there
//
// IMPORTANT NOTES:
// - Each printer holds ID labels or shipping labels (its media)
// =========================================================
public sealed class ListOperationsLabelPrintersResponse
{
    private ListOperationsLabelPrintersResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsLabelPrinter> Printers { get; private init; } = Array.Empty<OperationsLabelPrinter>();

    public static ListOperationsLabelPrintersResponse Success(IReadOnlyList<OperationsLabelPrinter> printers) => new() { Succeeded = true, Printers = printers };

    public static ListOperationsLabelPrintersResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new ListOperationsLabelPrintersResponse { Error = error };
    }
}
