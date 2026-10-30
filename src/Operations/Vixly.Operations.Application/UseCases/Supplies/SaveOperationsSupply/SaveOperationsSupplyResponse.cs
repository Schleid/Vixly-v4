using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupply;

// =========================================================
// FILE: SaveOperationsSupplyResponse.cs
//
// PURPOSE:
// Create a shipping supply with its first price, or change its details and barcodes (Slice 5E, S-1 to S-3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsSupplyResponse
{
    private SaveOperationsSupplyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid SupplyId { get; private init; }

    public static SaveOperationsSupplyResponse Success(Guid supplyId)
    {
        return new SaveOperationsSupplyResponse { Succeeded = true, SupplyId = supplyId };
    }

    public static SaveOperationsSupplyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsSupplyResponse { Error = error };
    }
}
