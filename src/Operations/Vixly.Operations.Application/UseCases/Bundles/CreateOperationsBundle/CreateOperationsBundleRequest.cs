using Vixly.Operations.Domain.Bundles;

namespace Vixly.Operations.Application.UseCases.Bundles.CreateOperationsBundle;

// =========================================================
// FILE: CreateOperationsBundleRequest.cs
//
// PURPOSE:
// Open a new bundle for a destination and a service (Slice 6A, D-2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CreateOperationsBundleRequest
{
    public Guid WarehouseId { get; init; }

    // A destination warehouse, or none and a country for home delivery.
    public Guid? DestinationWarehouseId { get; init; }

    public string? CountryCode { get; init; }

    public string? ServiceCode { get; init; }

    public string? TypeName { get; init; }

    public OperationsBundleBasis Basis { get; init; }

    // In the company's weight unit.
    public decimal? Target { get; init; }

    public Guid? RequestKey { get; init; }
}
