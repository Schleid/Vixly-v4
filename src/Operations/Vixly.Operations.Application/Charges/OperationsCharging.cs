using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsCharging.cs
//
// PURPOSE:
// Small rules shared by the pricing use cases (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Read a destination country
// - Turn the day staff choose into the moment new prices start: midnight in the company's time zone
//
// IMPORTANT NOTES:
// - Today (or no day) means now; a day already past is refused
// =========================================================
public static class OperationsCharging
{
    public static string? Country(string? value)
    {
        try
        {
            return OperationsCountryCodes.Normalize(value);
        }
        catch (DomainException)
        {
            return null;
        }
    }

    // Null: starts now.
    public static DateTimeOffset? Start(DateOnly? day, OperationsTenantSettings settings, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (day is not { } chosen)
        {
            return null;
        }

        if (OperationsTimeZones.StartOfDay(chosen.AddDays(1), settings.TimeZone) <= utcNow)
        {
            throw new DomainException(OperationsChargeErrors.EffectiveInvalid);
        }

        DateTimeOffset start = OperationsTimeZones.StartOfDay(chosen, settings.TimeZone);
        return start <= utcNow ? null : start;
    }

    public static bool Same(OperationsPriceVersion version, OperationsRatePrices prices)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(prices);

        return version.MinimumCharge == prices.MinimumCharge && version.ServiceFee == prices.ServiceFee && version.ConsolidationFee == prices.ConsolidationFee
            && version.PerPackage == prices.PerPackage && version.PerShipment == prices.PerShipment
            && version.Bands.Count == prices.Bands.Count && version.Items.Count == prices.Items.Count
            && version.Bands.Zip(prices.Bands.OrderBy(band => band.From)).All(pair => pair.First.From == pair.Second.From && pair.First.Kind == pair.Second.Kind
                && pair.First.Amount == pair.Second.Amount && pair.First.Base == pair.Second.Base)
            && prices.Items.All(item => version.PriceOf(item.ItemTypeId) == item.Price);
    }
}
