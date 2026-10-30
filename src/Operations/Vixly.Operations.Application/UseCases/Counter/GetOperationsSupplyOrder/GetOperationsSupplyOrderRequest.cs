using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Counter.GetOperationsSupplyOrder;

// =========================================================
// FILE: GetOperationsSupplyOrderRequest.cs
//
// PURPOSE:
// An order of supplies at the counter (Slice 5E, S-14): its lines, totals, the customer and the discount policy.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsSupplyOrderRequest
{
    public Guid SaleId { get; init; }
}
