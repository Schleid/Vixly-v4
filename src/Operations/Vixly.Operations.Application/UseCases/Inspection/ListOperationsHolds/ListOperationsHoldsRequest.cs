using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.UseCases.Inspection.ListOperationsHolds;

// =========================================================
// FILE: ListOperationsHoldsRequest.cs
//
// PURPOSE:
// Open holds at the warehouses a person covers, oldest first, and the disposals awaiting approval (Slice 3C design section 6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsHoldsRequest
{
    public OperationsHoldReasons Reason { get; init; } = OperationsHoldReasons.None;

    public Guid? WarehouseId { get; init; }

    public int Page { get; init; } = 1;
}
