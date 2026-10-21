using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsRateCard.cs
//
// PURPOSE:
// A price list: one service on one route (origin and destination warehouses, or any), for the company or one customer, with dated versions (Slice 4B, routes in 4C).
//
// RESPONSIBILITIES:
// - Keep the service, route, customer, customs policy and basis (weight or size, from the service)
// - Schedule a version, remove a scheduled one, find the version in force at a moment
//
// IMPORTANT NOTES:
// - One price list per service, route and customer (a unique index)
// - A version that has started never changes (OperationsVersions)
// - Item prices on ocean lists only; flat prices per package or shipment on ground lists only
// - Never deleted, taken out of use
// =========================================================
public sealed class OperationsRateCard : OperationsEntity
{
    private readonly List<OperationsPriceVersion> _versions = new();

    private OperationsRateCard()
    {
        ServiceCode = string.Empty;
    }

    private OperationsRateCard(Guid tenantId, string serviceCode, Guid? originWarehouseId, Guid? destinationWarehouseId, Guid? customerId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        (ServiceCode, OriginWarehouseId, DestinationWarehouseId, CustomerId, Basis) =
            (serviceCode, originWarehouseId, destinationWarehouseId, customerId, BasisFor(serviceCode));
    }

    public string ServiceCode { get; private set; }

    // Null: any origin (where the package was dropped off).
    public Guid? OriginWarehouseId { get; private set; }

    // Null: any destination.
    public Guid? DestinationWarehouseId { get; private set; }

    // Null: the company's standard prices.
    public Guid? CustomerId { get; private set; }

    public OperationsRateBasis Basis { get; private set; }

    public OperationsCustomsPolicy Customs { get; private set; } = OperationsCustomsPolicy.TransportOnly;

    public IReadOnlyList<OperationsPriceVersion> Versions => _versions;

    public bool IsActive { get; private set; }

    public OperationsTransportMode Mode => OperationsTransportModes.Of(ServiceCode);

    // Item prices belong to lists by size (ocean) only.
    public bool PricesItems => Basis == OperationsRateBasis.Volume;

    // Air and ground are priced by weight; ocean by size, and by item (a barrel, a car) (owner, 2026-10-19).
    public static OperationsRateBasis BasisFor(string serviceCode) =>
        OperationsTransportModes.Of(serviceCode) == OperationsTransportMode.Ocean ? OperationsRateBasis.Volume : OperationsRateBasis.Weight;

    public static OperationsRateCard Create(Guid tenantId, string? serviceCode, Guid? originWarehouseId, Guid? destinationWarehouseId, Guid? customerId,
        OperationsCustomsPolicy customs, OperationsRatePrices prices, DateTimeOffset? effectiveFrom, DateTimeOffset utcNow)
    {
        string service = serviceCode?.Trim() ?? string.Empty;

        if (service.Length is 0 or > OperationsChargeLimits.ServiceCodeMaxLength)
        {
            throw new DomainException(OperationsChargeErrors.ServiceUnknown);
        }

        OperationsRateCard card = new(tenantId, service, Optional(originWarehouseId), Optional(destinationWarehouseId), Optional(customerId), utcNow) { IsActive = true };
        card.ChangeCustoms(customs, utcNow);
        card.Schedule(prices, effectiveFrom, utcNow);
        return card;
    }

    public OperationsPriceVersion Schedule(OperationsRatePrices prices, DateTimeOffset? effectiveFrom, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(prices);

        if (!PricesItems && prices.Items.Count > 0)
        {
            throw new DomainException(OperationsChargeErrors.ItemsInvalid);
        }

        // Flat prices per package or per shipment are a ground choice (owner: weight, a flat price, or both).
        if (Mode != OperationsTransportMode.Ground && (prices.PerPackage is not null || prices.PerShipment is not null))
        {
            throw new DomainException(OperationsChargeErrors.BandsInvalid);
        }

        OperationsPriceVersion version = OperationsPriceVersion.Create(prices, OperationsVersions.Start(effectiveFrom, utcNow));
        OperationsVersions.Schedule(_versions, version, utcNow);
        MarkAsModified(utcNow);
        return version;
    }

    public void ChangeCustoms(OperationsCustomsPolicy customs, DateTimeOffset utcNow)
    {
        Customs = Enum.IsDefined(customs) ? customs : throw new DomainException(OperationsChargeErrors.ChoiceInvalid);
        MarkAsModified(utcNow);
    }

    public void RemoveScheduled(Guid versionId, DateTimeOffset utcNow)
    {
        OperationsVersions.Remove(_versions, versionId, utcNow);
        MarkAsModified(utcNow);
    }

    public void ChangeStatus(bool active, DateTimeOffset utcNow)
    {
        IsActive = active;
        MarkAsModified(utcNow);
    }

    public OperationsPriceVersion? VersionAt(DateTimeOffset at) => OperationsVersions.At(_versions, at);

    private static Guid? Optional(Guid? id) => id == Guid.Empty ? null : id;
}
