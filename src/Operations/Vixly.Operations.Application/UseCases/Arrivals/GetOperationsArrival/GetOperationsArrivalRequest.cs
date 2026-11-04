namespace Vixly.Operations.Application.UseCases.Arrivals.GetOperationsArrival;

// =========================================================
// FILE: GetOperationsArrivalRequest.cs
//
// PURPOSE:
// One dispatch received at a destination warehouse: its bundles and what did not match (Slice 7, R-1 and R-2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsArrivalRequest
{
    public Guid DispatchId { get; init; }

    public Guid WarehouseId { get; init; }
}
