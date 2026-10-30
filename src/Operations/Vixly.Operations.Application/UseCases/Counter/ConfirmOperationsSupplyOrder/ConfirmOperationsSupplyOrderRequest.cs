using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Counter.ConfirmOperationsSupplyOrder;

// =========================================================
// FILE: ConfirmOperationsSupplyOrderRequest.cs
//
// PURPOSE:
// An order of supplies confirmed at the counter (Slice 5E, S-14 and S-15): it gets its number and its stock moves; it is then to pay.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ConfirmOperationsSupplyOrderRequest
{
    public Guid SaleId { get; init; }
}
