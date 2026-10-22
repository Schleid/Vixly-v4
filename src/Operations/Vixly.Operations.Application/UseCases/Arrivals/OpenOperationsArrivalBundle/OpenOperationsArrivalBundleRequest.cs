namespace Vixly.Operations.Application.UseCases.Arrivals.OpenOperationsArrivalBundle;

// =========================================================
// FILE: OpenOperationsArrivalBundleRequest.cs
//
// PURPOSE:
// Open a bundle at its destination, so its parcels can be scanned (Slice 7, R-1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class OpenOperationsArrivalBundleRequest
{
    public Guid DispatchId { get; init; }

    public Guid WarehouseId { get; init; }

    public Guid BundleId { get; init; }
}
