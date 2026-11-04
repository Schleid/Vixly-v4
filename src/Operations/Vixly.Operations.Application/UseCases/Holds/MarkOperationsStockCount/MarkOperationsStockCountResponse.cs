using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Holds.MarkOperationsStockCount;

// =========================================================
// FILE: MarkOperationsStockCountResponse.cs
//
// PURPOSE:
// Marks a parcel of a count found on its shelf (picked or scanned) or missing, with a note (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class MarkOperationsStockCountResponse
{
    private MarkOperationsStockCountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static MarkOperationsStockCountResponse Success()
    {
        return new MarkOperationsStockCountResponse { Succeeded = true };
    }

    public static MarkOperationsStockCountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new MarkOperationsStockCountResponse { Error = error };
    }
}
