using System.Globalization;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.UseCases.Reports.RunOperationsReport;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Resources;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Web.Components.Reports;

// =========================================================
// FILE: OperationsReportWords.cs
//
// PURPOSE:
// Words and figures of a report run (Slice 11), for the page and the files.
//
// RESPONSIBILITIES:
// - Name a report, a column and a row; show a figure by its kind (count, money, days, hours, percent, weight)
// - Build the sheet (title, notes, headings, rows, totals, previous period) for CSV, Excel and PDF
// - The change against the period before
//
// IMPORTANT NOTES:
// - Text comes from OperationsText (L-10); money columns are left out for whoever may not see them
// =========================================================
public static class OperationsReportWords
{
    public static string Title(IStringLocalizer<OperationsText> text, OperationsReportDefinition report) => text["ui.reports.report." + report.Key].Value;

    public static IReadOnlyList<(OperationsReportColumn Column, int Index)> Shown(OperationsReportDefinition report, bool seesMoney) =>
        report.Columns.Select((column, index) => (column, index)).Where(item => seesMoney || !item.column.IsMoney).ToList();

    public static string Header(IStringLocalizer<OperationsText> text, OperationsReportColumn column) => text["ui.reports.column." + column.Key].Value;

    public static string Label(IStringLocalizer<OperationsText> text, OperationsReportLabel label, OperationsReportGrain grain)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(label);

        if (label.Date is { } date)
        {
            return grain switch
            {
                OperationsReportGrain.Month => date.ToString("MMMM yyyy", CultureInfo.CurrentCulture),
                OperationsReportGrain.Week => VixlyText.Format(text, "ui.reports.week_of", "day", date.ToString("d", CultureInfo.CurrentCulture)),
                _ => date.ToString("ddd d MMM yyyy", CultureInfo.CurrentCulture)
            };
        }

        return label.TextKey is { } key ? text[key].Value + (label.Text is { } extra ? " · " + extra : string.Empty) : label.Text ?? string.Empty;
    }

    public static string Value(decimal? value, OperationsReportColumn column, string currency, OperationsWeightUnit unit) => value is not { } number ? "—" : column.Value switch
    {
        OperationsReportValue.Money => number.ToString("N2", CultureInfo.CurrentCulture) + " " + currency,
        OperationsReportValue.Days or OperationsReportValue.Hours => number.ToString("N1", CultureInfo.CurrentCulture),
        OperationsReportValue.Percent => number.ToString("N1", CultureInfo.CurrentCulture) + " %",
        OperationsReportValue.Weight => OperationsUnits.ToShownWeight(number, unit).ToString("N1", CultureInfo.CurrentCulture) + (unit == OperationsWeightUnit.Pound ? " lb" : " kg"),
        _ => number.ToString("N0", CultureInfo.CurrentCulture)
    };

    // The number a spreadsheet gets: weights in the company's unit, the rest as worked out.
    public static decimal? Number(decimal? value, OperationsReportColumn column, OperationsWeightUnit unit) =>
        value is { } number && column.Value == OperationsReportValue.Weight ? Math.Round(OperationsUnits.ToShownWeight(number, unit), 2) : value;

    // The change against the period before, for counts and money (none when there was nothing before).
    public static string Change(decimal? now, decimal? before, OperationsReportColumn column) =>
        column.Value is OperationsReportValue.Count or OperationsReportValue.Money or OperationsReportValue.Weight && now is { } current && before is { } previous && previous != 0m
            ? ((current - previous) / previous * 100m).ToString("+0.0;-0.0;0", CultureInfo.CurrentCulture) + " %"
            : string.Empty;

    public static OperationsReportSheet Sheet(IStringLocalizer<OperationsText> text, RunOperationsReportResponse run, string? company)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(run);
        OperationsReportDefinition report = run.Report!;
        IReadOnlyList<(OperationsReportColumn Column, int Index)> shown = Shown(report, run.SeesMoney);
        string place = run.WarehouseId is { } id ? run.Places.FirstOrDefault(item => item.Id == id)?.Name ?? string.Empty : text["ui.reports.all_warehouses"].Value;
        List<string> notes = new()
        {
            company ?? string.Empty,
            report.AsOf ? VixlyText.Format(text, "ui.reports.as_of", "day", run.To.ToString("d", CultureInfo.CurrentCulture))
                : Period(text, "ui.reports.period", run.From, run.To),
            place
        };
        List<IReadOnlyList<OperationsReportCell>> rows = run.Table!.Rows.Select(row => (IReadOnlyList<OperationsReportCell>)new[] { new OperationsReportCell(Label(text, row.Label, run.Grain)) }
            .Concat(shown.Select(item => new OperationsReportCell(Value(row.Values[item.Index], item.Column, run.CurrencyCode, run.WeightUnit),
                Number(row.Values[item.Index], item.Column, run.WeightUnit)))).ToList()).ToList();
        rows.Add(new[] { new OperationsReportCell(text["ui.reports.total"].Value) }.Concat(shown.Select(item => new OperationsReportCell(
            Value(run.Table.Totals[item.Index], item.Column, run.CurrencyCode, run.WeightUnit), Number(run.Table.Totals[item.Index], item.Column, run.WeightUnit)))).ToList());

        if (run.Previous is { } previous)
        {
            rows.Add(new[] { new OperationsReportCell(text["ui.reports.previous"].Value) }.Concat(shown.Select(item => new OperationsReportCell(
                Value(previous[item.Index], item.Column, run.CurrencyCode, run.WeightUnit), Number(previous[item.Index], item.Column, run.WeightUnit)))).ToList());
        }

        return new OperationsReportSheet(Title(text, report), notes, new[] { text[report.OverTime ? "ui.reports.when" : "ui.reports.what"].Value }
            .Concat(shown.Select(item => Header(text, item.Column))).ToList(), rows);
    }

    public static string Period(IStringLocalizer<OperationsText> text, string key, DateOnly? from, DateOnly? to) =>
        VixlyText.Format(text, key, new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["from"] = from?.ToString("d", CultureInfo.CurrentCulture) ?? string.Empty,
            ["to"] = to?.ToString("d", CultureInfo.CurrentCulture) ?? string.Empty
        });

    public static string Query(RunOperationsReportResponse run) =>
        "from=" + OperationsReportPeriods.Iso(run.From) + "&to=" + OperationsReportPeriods.Iso(run.To) + "&grain=" + run.Grain.ToString().ToLowerInvariant()
        + "&by=" + run.Dimension.ToString().ToLowerInvariant() + (run.WarehouseId is { } id ? "&warehouse=" + id : string.Empty)
        + (run.Report?.UsesDays == true ? "&days=" + run.Days.ToString(CultureInfo.InvariantCulture) : string.Empty);
}
