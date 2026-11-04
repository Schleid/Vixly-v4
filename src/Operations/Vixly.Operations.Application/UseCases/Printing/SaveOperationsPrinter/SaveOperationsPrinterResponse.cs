using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.SaveOperationsPrinter;

// =========================================================
// FILE: SaveOperationsPrinterResponse.cs
//
// PURPOSE:
// Adds a label printer to a warehouse, or changes one (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsPrinterResponse
{
    private SaveOperationsPrinterResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PrinterId { get; private init; }

    public static SaveOperationsPrinterResponse Success(Guid printerId)
    {
        return new SaveOperationsPrinterResponse { Succeeded = true, PrinterId = printerId };
    }

    public static SaveOperationsPrinterResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsPrinterResponse { Error = error };
    }
}
