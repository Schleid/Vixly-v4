using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.ReceiveOperationsSupplyTransfer;

// =========================================================
// FILE: ReceiveOperationsSupplyTransferRequest.cs
//
// PURPOSE:
// Supplies of a transfer received at the warehouse they were sent to (Slice 5E, S-6): the stock arrives as sent.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReceiveOperationsSupplyTransferRequest
{
    public Guid TransferId { get; init; }
}
