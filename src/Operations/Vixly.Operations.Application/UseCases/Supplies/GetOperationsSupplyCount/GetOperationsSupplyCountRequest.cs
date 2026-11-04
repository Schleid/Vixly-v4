using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupplyCount;

// =========================================================
// FILE: GetOperationsSupplyCountRequest.cs
//
// PURPOSE:
// A stock count with the supplies to count and their stock on hand (Slice 5E, S-7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsSupplyCountRequest
{
    public Guid CountId { get; init; }
}
