using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Counter.StartOperationsSupplyOrder;

// =========================================================
// FILE: StartOperationsSupplyOrderRequest.cs
//
// PURPOSE:
// An order of supplies on its own for a customer at the counter (Slice 5E, S-14): the open one, or a new one.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class StartOperationsSupplyOrderRequest
{
    public Guid CustomerId { get; init; }

    // The counter's warehouse; none: the first one the clerk serves at.
    public Guid? WarehouseId { get; init; }
}
