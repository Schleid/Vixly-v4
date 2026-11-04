namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsShelfCounts;

// =========================================================
// FILE: GetOperationsShelfCountsRequest.cs
//
// PURPOSE:
// The shelf counts of the warehouses a person may count, or one of them (Slice 12D, owner H-7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsShelfCountsRequest
{
    // Empty: the latest counts.
    public Guid? CountId { get; init; }
}
