using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Holds.CloseOperationsStockCount;

// =========================================================
// FILE: CloseOperationsStockCountResponse.cs
//
// PURPOSE:
// Closes a count once every parcel is marked; a parcel missing needs a note and the Owner or an administrator (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CloseOperationsStockCountResponse
{
    private CloseOperationsStockCountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CloseOperationsStockCountResponse Success()
    {
        return new CloseOperationsStockCountResponse { Succeeded = true };
    }

    public static CloseOperationsStockCountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CloseOperationsStockCountResponse { Error = error };
    }
}
