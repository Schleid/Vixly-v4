using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.SendOperationsSupplyTransfer;

// =========================================================
// FILE: SendOperationsSupplyTransferResponse.cs
//
// PURPOSE:
// Supplies sent from a warehouse to another of the company's (Slice 5E, S-6): the stock leaves at once and arrives when received.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SendOperationsSupplyTransferResponse
{
    private SendOperationsSupplyTransferResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid TransferId { get; private init; }

    public static SendOperationsSupplyTransferResponse Success(Guid transferId)
    {
        return new SendOperationsSupplyTransferResponse { Succeeded = true, TransferId = transferId };
    }

    public static SendOperationsSupplyTransferResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SendOperationsSupplyTransferResponse { Error = error };
    }
}
