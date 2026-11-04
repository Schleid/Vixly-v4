using System.Globalization;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Resources;

namespace Vixly.Operations.Web.Components.Bundles;

// =========================================================
// FILE: OperationsBundleWords.cs
//
// PURPOSE:
// Words the bundle pages share (Slice 6A).
//
// RESPONSIBILITIES:
// - Write weights and sizes in the company's units
// - Name a bundle's destination and its fill against its target
//
// IMPORTANT NOTES:
// - Text comes from OperationsText (L-10)
// =========================================================
public static class OperationsBundleWords
{
    public static string Weight(decimal kilograms, OperationsWeightUnit unit) =>
        OperationsUnits.ToShownWeight(kilograms, unit).ToString("0.##", CultureInfo.CurrentCulture) + (unit == OperationsWeightUnit.Pound ? " lb" : " kg");

    public static string Size(decimal? length, decimal? width, decimal? height, OperationsDimensionUnit unit) =>
        length is { } l && width is { } w && height is { } h
            ? string.Join(" × ", new[] { l, w, h }.Select(side => OperationsUnits.ToShownLength(side, unit).ToString("0.#", CultureInfo.CurrentCulture))) + (unit == OperationsDimensionUnit.Inch ? " in" : " cm")
            : "—";

    // A destination warehouse by its code, or a country for home delivery.
    public static string Destination(IStringLocalizer<OperationsText> text, Guid? warehouseId, string countryCode, IEnumerable<OperationsWarehouse> warehouses)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(warehouses);
        return warehouseId is { } id && warehouses.FirstOrDefault(warehouse => warehouse.Id == id) is { } found
            ? found.Code + " · " + found.Name
            : Vixly.Shared.Localization.VixlyText.Format(text, "ui.bundles.home_delivery", "country", countryCode);
    }

    // The form value of a destination: a warehouse, or a country for home delivery.
    public static string DestinationValue(Guid? warehouseId, string countryCode) => warehouseId is { } id ? "w:" + id.ToString("N") : "c:" + countryCode;

    public static string Fill(IStringLocalizer<OperationsText> text, OperationsBundle bundle, OperationsWeightUnit unit)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(bundle);
        string filled = Weight(bundle.Filled, unit);
        return bundle.TargetKg is { } target
            ? Vixly.Shared.Localization.VixlyText.Format(text, "ui.bundles.fill_of", new Dictionary<string, object> { ["filled"] = filled, ["target"] = Weight(target, unit) })
            : filled;
    }
}
