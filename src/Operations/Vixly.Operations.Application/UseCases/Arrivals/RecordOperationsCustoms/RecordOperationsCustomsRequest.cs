namespace Vixly.Operations.Application.UseCases.Arrivals.RecordOperationsCustoms;

// =========================================================
// FILE: RecordOperationsCustomsRequest.cs
//
// PURPOSE:
// Record customs for a dispatch or one of its bundles at a warehouse that clears customs (Slice 7, R-3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RecordOperationsCustomsRequest
{
    public Guid DispatchId { get; init; }

    public Guid WarehouseId { get; init; }

    public Guid? BundleId { get; init; }

    public string? Step { get; init; }

    public string? Message { get; init; }

    public Guid? RequestKey { get; init; }

    public string? PortalSlug { get; init; }
}
