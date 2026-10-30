namespace Vixly.Operations.Application.UseCases.Arrivals.ScanOperationsArrival;

// =========================================================
// FILE: ScanOperationsArrivalRequest.cs
//
// PURPOSE:
// Scan a bundle, a repack box or a parcel arriving at a destination warehouse (Slice 7, R-1, R-2, R-11).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ScanOperationsArrivalRequest
{
    public Guid DispatchId { get; init; }

    public Guid WarehouseId { get; init; }

    public string? Code { get; init; }

    public string? PortalSlug { get; init; }
}
