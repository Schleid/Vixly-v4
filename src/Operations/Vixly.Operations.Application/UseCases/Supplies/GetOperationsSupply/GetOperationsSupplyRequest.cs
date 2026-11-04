using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupply;

// =========================================================
// FILE: GetOperationsSupplyRequest.cs
//
// PURPOSE:
// One shipping supply with its barcodes and prices (Slice 5E, S-3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsSupplyRequest
{
    public Guid SupplyId { get; init; }
}
