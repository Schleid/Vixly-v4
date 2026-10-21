using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.Supplies;

// =========================================================
// FILE: OperationsSupplyPricing.cs
//
// PURPOSE:
// Builds a supply price from a form (Slice 5E, S-2), for the new supply and the new price.
//
// RESPONSIBILITIES:
// - Turn the day it starts into the moment in the company's time zone
// - Check that each warehouse priced is one of the company's
//
// IMPORTANT NOTES:
// - Throws DomainException for a refusal; the use cases turn it into a response
// =========================================================
public static class OperationsSupplyPricing
{
    public static async Task<OperationsSupplyPrice> PriceAsync(
        decimal? price,
        IReadOnlyList<OperationsSupplyBreak> breaks,
        IReadOnlyList<OperationsSupplyWarehousePrice> warehousePrices,
        DateOnly? effectiveFrom,
        IOperationsSettingsRepository settings,
        IOperationsLocationRepository locations,
        Guid tenantId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(breaks);
        ArgumentNullException.ThrowIfNull(warehousePrices);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(locations);

        if (price is not { } unit)
        {
            throw new DomainException(OperationsSupplyErrors.PriceInvalid);
        }

        if (warehousePrices.Count > 0)
        {
            HashSet<Guid> known = (await locations.ListWarehousesAsync(cancellationToken)).Select(warehouse => warehouse.Id).ToHashSet();

            if (warehousePrices.Any(place => !known.Contains(place.WarehouseId)))
            {
                throw new DomainException(OperationsSupplyErrors.WarehousePricesInvalid);
            }
        }

        OperationsTenantSettings company = await settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(tenantId, utcNow);
        DateTimeOffset start = OperationsVersions.Start(OperationsCharging.Start(effectiveFrom, company, utcNow), utcNow);
        return OperationsSupplyPrice.Create(unit, breaks, warehousePrices, start);
    }
}
