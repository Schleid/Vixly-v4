using System.Globalization;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Resources;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Web.Site;

// =========================================================
// FILE: OperationsSitePrices.cs
//
// PURPOSE:
// How published prices read on the company's website home page (landing page rates, owner 2026-10-30).
//
// RESPONSIBILITIES:
// - Give the unit of a price list (lb, kg, ft³ or m³), a money amount and one band as a sentence
//
// IMPORTANT NOTES:
// - Same wording as the Pricing page (ui.charges.band.*), so the two pages never disagree
// =========================================================
public static class OperationsSitePrices
{
    public static string Unit(OperationsPriceListView list, OperationsPricingView pricing)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(pricing);
        return list.Basis == OperationsRateBasis.Volume
            ? (pricing.DimensionUnit == OperationsDimensionUnit.Inch ? "ft³" : "m³")
            : WeightUnit(pricing);
    }

    public static string WeightUnit(OperationsPricingView pricing)
    {
        ArgumentNullException.ThrowIfNull(pricing);
        return pricing.WeightUnit == OperationsWeightUnit.Pound ? "lb" : "kg";
    }

    public static string Money(decimal value, string currency) => value.ToString("N2", CultureInfo.CurrentCulture) + " " + currency;

    public static string Band(IStringLocalizer<OperationsText> text, OperationsRateBand band, OperationsPriceListView list, OperationsPricingView pricing)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(band);
        string unit = Unit(list, pricing);
        return VixlyText.Format(text, "ui.charges.band." + band.Kind, new Dictionary<string, object>
        {
            ["from"] = band.From.ToString("0.##", CultureInfo.CurrentCulture) + " " + unit,
            ["amount"] = Money(band.Amount, pricing.CurrencyCode),
            ["base"] = Money(band.Base ?? 0m, pricing.CurrencyCode),
            ["unit"] = unit
        });
    }
}
