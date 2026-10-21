using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Application.UseCases.Charges.SaveOperationsPriceList;

// =========================================================
// FILE: SaveOperationsPriceListRequest.cs
//
// PURPOSE:
// Adds a price list, or schedules a new version of one, and takes it in or out of use (Slice 4B design section 5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsPriceListRequest
{
    public Guid? CardId { get; init; }

    public string? ServiceCode { get; init; }

    // Slice 4C: the route; null: any origin, any destination.
    public Guid? OriginWarehouseId { get; init; }

    public Guid? DestinationWarehouseId { get; init; }

    public OperationsCustomsPolicy Customs { get; init; } = OperationsCustomsPolicy.TransportOnly;

    public string? CustomerCode { get; init; }

    public IReadOnlyList<OperationsRateBand>? Bands { get; init; }

    public IReadOnlyList<OperationsRateItem>? Items { get; init; }

    public decimal? MinimumCharge { get; init; }

    public decimal? ServiceFee { get; init; }

    public decimal? ConsolidationFee { get; init; }

    // Slice 4C, ground: flat prices per package and per shipment.
    public decimal? PerPackage { get; init; }

    public decimal? PerShipment { get; init; }

    public DateOnly? EffectiveFrom { get; init; }

    public bool Active { get; init; } = true;
}
