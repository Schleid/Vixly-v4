namespace Vixly.Operations.Application.UseCases.Arrivals.FinishOperationsArrival;

// =========================================================
// FILE: FinishOperationsArrivalRequest.cs
//
// PURPOSE:
// Finish receiving a dispatch: what did not come is listed as missing (Slice 7, R-2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class FinishOperationsArrivalRequest
{
    public Guid DispatchId { get; init; }

    public Guid WarehouseId { get; init; }
}
