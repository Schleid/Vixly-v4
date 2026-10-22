using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.PrintOperationsParcelLabel;

// =========================================================
// FILE: PrintOperationsParcelLabelResponse.cs
//
// PURPOSE:
// Prints a parcel's ID label again on a dock printer, or a test label (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PrintOperationsParcelLabelResponse
{
    private PrintOperationsParcelLabelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PrinterId { get; private init; }

    public static PrintOperationsParcelLabelResponse Success(Guid printerId)
    {
        return new PrintOperationsParcelLabelResponse { Succeeded = true, PrinterId = printerId };
    }

    public static PrintOperationsParcelLabelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PrintOperationsParcelLabelResponse { Error = error };
    }
}
