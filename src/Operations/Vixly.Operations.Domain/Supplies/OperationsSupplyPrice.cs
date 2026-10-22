using Vixly.Operations.Domain.Charges;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyPrice.cs
//
// PURPOSE:
// A price of a supply from the day it starts (Slice 5E, S-2), as the price versions of Slice 4B.
//
// RESPONSIBILITIES:
// - Carry the company unit price, the quantity breaks and the prices per warehouse
// - Give the unit price for a warehouse and a quantity
//
// IMPORTANT NOTES:
// - Before tax; at most two decimals (money)
// - A warehouse price replaces the company price there, breaks included (they apply to the company price only)
// - Breaks go up in quantity and down in price
// =========================================================
public sealed class OperationsSupplyPrice : IOperationsVersion
{
    private readonly List<OperationsSupplyBreak> _breaks = new();

    private readonly List<OperationsSupplyWarehousePrice> _warehouses = new();

    private OperationsSupplyPrice()
    {
    }

    public Guid Id { get; private set; }

    public DateTimeOffset EffectiveFromUtc { get; private set; }

    public decimal Price { get; private set; }

    public IReadOnlyList<OperationsSupplyBreak> Breaks => _breaks;

    public IReadOnlyList<OperationsSupplyWarehousePrice> Warehouses => _warehouses;

    public static OperationsSupplyPrice Create(decimal price, IReadOnlyList<OperationsSupplyBreak>? breaks, IReadOnlyList<OperationsSupplyWarehousePrice>? warehouses,
        DateTimeOffset effectiveFromUtc)
    {
        decimal unit = OperationsChargeLimits.Money(price, OperationsSupplyErrors.PriceInvalid);
        List<OperationsSupplyBreak> steps = (breaks ?? Array.Empty<OperationsSupplyBreak>()).OrderBy(step => step.FromQuantity).ToList();
        List<OperationsSupplyWarehousePrice> places = (warehouses ?? Array.Empty<OperationsSupplyWarehousePrice>()).ToList();

        // Each break from 2 or more, a larger quantity than the one before, at a lower price than the one before.
        decimal previous = unit;
        int from = 1;

        foreach (OperationsSupplyBreak step in steps)
        {
            if (steps.Count > OperationsSupplyLimits.MaxBreaks || step.FromQuantity <= from || step.FromQuantity > OperationsSupplyLimits.MaxQuantity
                || OperationsChargeLimits.Money(step.Price, OperationsSupplyErrors.BreaksInvalid) >= previous)
            {
                throw new DomainException(OperationsSupplyErrors.BreaksInvalid);
            }

            (from, previous) = (step.FromQuantity, step.Price);
        }

        if (places.Count > OperationsSupplyLimits.MaxWarehousePrices || places.Any(place => place.WarehouseId == Guid.Empty)
            || places.Select(place => place.WarehouseId).Distinct().Count() != places.Count)
        {
            throw new DomainException(OperationsSupplyErrors.WarehousePricesInvalid);
        }

        foreach (OperationsSupplyWarehousePrice place in places)
        {
            OperationsChargeLimits.Money(place.Price, OperationsSupplyErrors.WarehousePricesInvalid);
        }

        OperationsSupplyPrice version = new() { Id = Guid.NewGuid(), EffectiveFromUtc = effectiveFromUtc, Price = unit };
        version._breaks.AddRange(steps);
        version._warehouses.AddRange(places);
        return version;
    }

    // The unit price at this warehouse for this quantity.
    public decimal UnitPrice(Guid? warehouseId, int quantity)
    {
        if (warehouseId is { } id && _warehouses.FirstOrDefault(place => place.WarehouseId == id) is { } place)
        {
            return place.Price;
        }

        return _breaks.Where(step => step.FromQuantity <= quantity).OrderByDescending(step => step.FromQuantity).Select(step => (decimal?)step.Price).FirstOrDefault() ?? Price;
    }
}
