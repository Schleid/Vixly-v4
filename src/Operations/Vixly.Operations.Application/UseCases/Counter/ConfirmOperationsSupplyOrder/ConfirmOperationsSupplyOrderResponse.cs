using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.ConfirmOperationsSupplyOrder;

// =========================================================
// FILE: ConfirmOperationsSupplyOrderResponse.cs
//
// PURPOSE:
// An order of supplies confirmed at the counter (Slice 5E, S-14 and S-15): it gets its number and its stock moves; it is then to pay.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ConfirmOperationsSupplyOrderResponse
{
    private ConfirmOperationsSupplyOrderResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string Number { get; private init; } = string.Empty;

    public static ConfirmOperationsSupplyOrderResponse Success(string number)
    {
        return new ConfirmOperationsSupplyOrderResponse { Succeeded = true, Number = number };
    }

    public static ConfirmOperationsSupplyOrderResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ConfirmOperationsSupplyOrderResponse { Error = error };
    }
}
