using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.ReceiveOperationsSupplyTransfer;

// =========================================================
// FILE: ReceiveOperationsSupplyTransferResponse.cs
//
// PURPOSE:
// Supplies of a transfer received at the warehouse they were sent to (Slice 5E, S-6): the stock arrives as sent.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReceiveOperationsSupplyTransferResponse
{
    private ReceiveOperationsSupplyTransferResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReceiveOperationsSupplyTransferResponse Success()
    {
        return new ReceiveOperationsSupplyTransferResponse { Succeeded = true };
    }

    public static ReceiveOperationsSupplyTransferResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReceiveOperationsSupplyTransferResponse { Error = error };
    }
}
