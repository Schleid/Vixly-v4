using System.Globalization;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Resources;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Web.Components.Charges;

// =========================================================
// FILE: OperationsChargeWords.cs
//
// PURPOSE:
// Words and numbers of the charges and quotes pages (Slice 4).
//
// RESPONSIBILITIES:
// - Name a charge line, a service and a unit
// - Write amounts in the page's language with the currency
//
// IMPORTANT NOTES:
// - Text comes from OperationsText (L-10)
// =========================================================
public static class OperationsChargeWords
{
    // A text with several named values (L-10).
    public static string Text(IStringLocalizer<OperationsText> text, string key, params (string Name, object Value)[] values) =>
        VixlyText.Format(text, key, values.ToDictionary(pair => pair.Name, pair => pair.Value, StringComparer.Ordinal));

    public static string Money(decimal amount, string currency) => amount.ToString("N2", CultureInfo.CurrentCulture) + " " + currency;

    public static string Number(decimal? value) => value is { } number ? OperationsPriceText.Write(number) : string.Empty;

    public static string Service(IStringLocalizer<OperationsText> text, string code, string? label = null) => label ?? text["ui.services.name." + code].Value;

    public static string Weight(OperationsWeightUnit unit) => unit == OperationsWeightUnit.Pound ? "lb" : "kg";

    public static string Length(OperationsDimensionUnit unit) => unit == OperationsDimensionUnit.Inch ? "in" : "cm";

    public static string Volume(OperationsDimensionUnit unit) => unit == OperationsDimensionUnit.Inch ? "ft³" : "m³";

    public static string Line(IStringLocalizer<OperationsText> text, OperationsChargeLineKind kind, string? label, int? piece, decimal quantity, decimal? basis)
    {
        string name = label ?? text["ui.charges.line." + kind].Value;

        return kind switch
        {
            OperationsChargeLineKind.Freight => Text(text, "ui.charges.line.freight_of", ("piece", piece ?? 1), ("basis", Number(basis))),
            OperationsChargeLineKind.SmallPackage or OperationsChargeLineKind.Item => Text(text, "ui.charges.line.piece_of", ("name", name), ("piece", piece ?? 1)),
            OperationsChargeLineKind.Extra when quantity > 1 => name + " × " + Number(quantity),
            _ => name
        };
    }

    public static string Line(IStringLocalizer<OperationsText> text, OperationsChargeLine line) => Line(text, line.Kind, line.Label, line.Piece, line.Quantity, line.Basis);

    // "from 11: 65.00 USD", "from 31: 4.75 USD / lb", "from 30: 75.00 USD + 4.75 USD / lb above 30".
    public static string Band(IStringLocalizer<OperationsText> text, OperationsRateBand band, string unit, string currency) =>
        Text(text, "ui.charges.band." + band.Kind, ("from", Number(band.From)), ("amount", Money(band.Amount, currency)), ("base", Money(band.Base ?? 0m, currency)), ("unit", unit));

    public static string Unit(OperationsRateBasis basis, OperationsWeightUnit weight, OperationsDimensionUnit dimension) =>
        basis == OperationsRateBasis.Volume ? Volume(dimension) : Weight(weight);
}
