namespace Vixly.Operations.Application.UseCases.Arrivals.GetOperationsArrivals;

// =========================================================
// FILE: GetOperationsArrivalsRequest.cs
//
// PURPOSE:
// The dispatches a destination warehouse receives (Slice 7, R-1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsArrivalsRequest
{
    public Guid? WarehouseId { get; init; }
}
