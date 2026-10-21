namespace Vixly.Operations.Application.UseCases.Dispatch.AddOperationsCarrierRate;

// =========================================================
// FILE: AddOperationsCarrierRateRequest.cs
//
// PURPOSE:
// Add a carrier's rate to a destination from a day on (Slice 6B, D-1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AddOperationsCarrierRateRequest
{
    public Guid CarrierId { get; init; }

    // A destination warehouse, or a country for home delivery.
    public Guid? DestinationWarehouseId { get; init; }

    public string? CountryCode { get; init; }

    public string? BundleType { get; init; }

    // Air: per the company's weight unit, with a minimum per shipment of the dispatch.
    public decimal? PerWeight { get; init; }

    public decimal? Minimum { get; init; }

    // Ocean: per bundle.
    public decimal? PerBundle { get; init; }

    public DateOnly StartsOn { get; init; }
}
