using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.StartOperationsSupplyOrder;

// =========================================================
// FILE: StartOperationsSupplyOrderResponse.cs
//
// PURPOSE:
// An order of supplies on its own for a customer at the counter (Slice 5E, S-14): the open one, or a new one.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class StartOperationsSupplyOrderResponse
{
    private StartOperationsSupplyOrderResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid SaleId { get; private init; }

    public static StartOperationsSupplyOrderResponse Success(Guid saleId)
    {
        return new StartOperationsSupplyOrderResponse { Succeeded = true, SaleId = saleId };
    }

    public static StartOperationsSupplyOrderResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new StartOperationsSupplyOrderResponse { Error = error };
    }
}
