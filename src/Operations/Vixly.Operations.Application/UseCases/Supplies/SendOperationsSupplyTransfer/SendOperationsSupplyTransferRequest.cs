using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.SendOperationsSupplyTransfer;

// =========================================================
// FILE: SendOperationsSupplyTransferRequest.cs
//
// PURPOSE:
// Supplies sent from a warehouse to another of the company's (Slice 5E, S-6): the stock leaves at once and arrives when received.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SendOperationsSupplyTransferRequest
{
    public Guid FromWarehouseId { get; init; }

    public Guid ToWarehouseId { get; init; }

    public IReadOnlyList<OperationsSupplyLine> Lines { get; init; } = Array.Empty<OperationsSupplyLine>();

    public string? Note { get; init; }

    // The form's key: the same form sent twice moves the stock once (ADR-0019).
    public Guid Key { get; init; }
}
