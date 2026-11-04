using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.CancelOperationsSupplyOrder;

// =========================================================
// FILE: CancelOperationsSupplyOrderResponse.cs
//
// PURPOSE:
// An order of supplies cancelled at the counter (Slice 5E, S-15): a confirmed one puts its stock back.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CancelOperationsSupplyOrderResponse
{
    private CancelOperationsSupplyOrderResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CancelOperationsSupplyOrderResponse Success()
    {
        return new CancelOperationsSupplyOrderResponse { Succeeded = true };
    }

    public static CancelOperationsSupplyOrderResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CancelOperationsSupplyOrderResponse { Error = error };
    }
}
