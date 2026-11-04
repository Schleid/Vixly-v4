using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Counter.CancelOperationsSupplyOrder;

// =========================================================
// FILE: CancelOperationsSupplyOrderRequest.cs
//
// PURPOSE:
// An order of supplies cancelled at the counter (Slice 5E, S-15): a confirmed one puts its stock back.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CancelOperationsSupplyOrderRequest
{
    public Guid SaleId { get; init; }
}
