using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.PrintOperationsShippingLabel;

// =========================================================
// FILE: PrintOperationsShippingLabelResponse.cs
//
// PURPOSE:
// Prints the shipping label of a cleared parcel on a printer holding 4 x 6 labels (Slice 4D step 2, A-5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PrintOperationsShippingLabelResponse
{
    private PrintOperationsShippingLabelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ParcelId { get; private init; }

    public bool Skipped { get; private init; }

    public static PrintOperationsShippingLabelResponse Success(Guid parcelId, bool skipped = false)
    {
        return new PrintOperationsShippingLabelResponse { Succeeded = true, ParcelId = parcelId, Skipped = skipped };
    }

    public static PrintOperationsShippingLabelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PrintOperationsShippingLabelResponse { Error = error };
    }
}
