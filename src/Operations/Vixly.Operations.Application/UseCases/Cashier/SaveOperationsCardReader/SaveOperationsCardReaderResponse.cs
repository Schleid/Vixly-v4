using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.SaveOperationsCardReader;

// =========================================================
// FILE: SaveOperationsCardReaderResponse.cs
//
// PURPOSE:
// Adds or removes a Stripe Terminal card reader of a warehouse (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsCardReaderResponse
{
    private SaveOperationsCardReaderResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsCardReaderResponse Success()
    {
        return new SaveOperationsCardReaderResponse { Succeeded = true };
    }

    public static SaveOperationsCardReaderResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsCardReaderResponse { Error = error };
    }
}
