using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.RemoveOperationsSupplyPrice;

// =========================================================
// FILE: RemoveOperationsSupplyPriceRequest.cs
//
// PURPOSE:
// Remove a supply's price that has not started yet (Slice 5E, S-2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RemoveOperationsSupplyPriceRequest
{
    public Guid SupplyId { get; init; }

    public Guid PriceId { get; init; }
}
