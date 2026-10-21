using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.RemoveOperationsSupplyPrice;

// =========================================================
// FILE: RemoveOperationsSupplyPriceResponse.cs
//
// PURPOSE:
// Remove a supply's price that has not started yet (Slice 5E, S-2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemoveOperationsSupplyPriceResponse
{
    private RemoveOperationsSupplyPriceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemoveOperationsSupplyPriceResponse Success()
    {
        return new RemoveOperationsSupplyPriceResponse { Succeeded = true };
    }

    public static RemoveOperationsSupplyPriceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemoveOperationsSupplyPriceResponse { Error = error };
    }
}
