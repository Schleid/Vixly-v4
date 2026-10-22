using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.AdjustOperationsSupplyStock;

// =========================================================
// FILE: AdjustOperationsSupplyStockResponse.cs
//
// PURPOSE:
// Supplies stock adjusted by hand at a warehouse (Slice 5E, S-4): more or fewer, with a reason and a note.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AdjustOperationsSupplyStockResponse
{
    private AdjustOperationsSupplyStockResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static AdjustOperationsSupplyStockResponse Success()
    {
        return new AdjustOperationsSupplyStockResponse { Succeeded = true };
    }

    public static AdjustOperationsSupplyStockResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AdjustOperationsSupplyStockResponse { Error = error };
    }
}
