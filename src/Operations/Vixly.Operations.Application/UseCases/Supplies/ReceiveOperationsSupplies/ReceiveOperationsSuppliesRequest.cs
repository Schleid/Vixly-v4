using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.ReceiveOperationsSupplies;

// =========================================================
// FILE: ReceiveOperationsSuppliesRequest.cs
//
// PURPOSE:
// Supplies received at a warehouse (Slice 5E, S-5): each line adds stock with its cost; the supplier and the reference are kept.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReceiveOperationsSuppliesRequest
{
    public Guid WarehouseId { get; init; }

    public IReadOnlyList<OperationsSupplyReceiptLine> Lines { get; init; } = Array.Empty<OperationsSupplyReceiptLine>();

    public string? Supplier { get; init; }

    // The supplier's invoice or delivery note.
    public string? Reference { get; init; }

    // The form's key: the same form sent twice moves the stock once (ADR-0019).
    public Guid Key { get; init; }
}
