using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.SaveOperationsPhonePrinter;

// =========================================================
// FILE: SaveOperationsPhonePrinterResponse.cs
//
// PURPOSE:
// A phone running Vixly Print adds or changes the portable printer next to it (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsPhonePrinterResponse
{
    private SaveOperationsPhonePrinterResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PrinterId { get; private init; }

    public static SaveOperationsPhonePrinterResponse Success(Guid printerId)
    {
        return new SaveOperationsPhonePrinterResponse { Succeeded = true, PrinterId = printerId };
    }

    public static SaveOperationsPhonePrinterResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsPhonePrinterResponse { Error = error };
    }
}
