using System.Globalization;
using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vixly.Operations.Application.Dispatch;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Resources;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Infrastructure.Documents;

// =========================================================
// FILE: OperationsManifestLayout.cs
//
// PURPOSE:
// Lays out the dispatch manifest and a bundle's manifest on Letter paper, landscape (Slice 6B, D-6).
//
// RESPONSIBILITIES:
// - The dispatch: route, service, cutoff, legs; then each bundle with its shipments, consignees, pieces, weights, values and contents
// - The bundle: its route, dispatch and shipments
//
// IMPORTANT NOTES:
// - Words from OperationsText in the current language; weights in the company's unit, times where the company works
// - A planned dispatch is marked draft
// =========================================================
internal static class OperationsManifestLayout
{
    public static byte[] RenderDispatch(OperationsDispatchManifest manifest, IStringLocalizer<OperationsText> text)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(text);
        string Word(string key) => text["ui.manifest." + key].Value;

        return Document.Create(pdf => pdf.Page(page =>
        {
            Setup(page);
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(manifest.CompanyName).FontSize(14).SemiBold();
                    column.Item().Text(manifest.OriginCode + " → " + manifest.DestinationCode + " · " + manifest.ServiceCode);

                    if (manifest.CutoffUtc is { } cutoff)
                    {
                        column.Item().Text(Word("cutoff") + ": " + Time(cutoff, manifest.TimeZone));
                    }
                });
                row.ConstantItem(260).Column(column =>
                {
                    column.Item().AlignRight().Text(Word("dispatch_title")).FontSize(14).SemiBold();
                    column.Item().AlignRight().Text(manifest.Number).FontSize(12);

                    if (manifest.Draft)
                    {
                        column.Item().AlignRight().Text(Word("draft")).FontColor("#B42318").SemiBold();
                    }
                });
            });
            page.Content().PaddingVertical(10).Column(column =>
            {
                column.Spacing(8);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(24);
                        columns.ConstantColumn(50);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });
                    table.Header(header =>
                    {
                        foreach (string key in new[] { "leg", "mode", "carrier", "from", "to", "reference", "departs", "arrives" })
                        {
                            header.Cell().BorderBottom(1).PaddingBottom(2).Text(Word(key)).SemiBold();
                        }
                    });

                    foreach (OperationsManifestLeg leg in manifest.Legs)
                    {
                        table.Cell().Text(leg.Sequence.ToString(CultureInfo.InvariantCulture));
                        table.Cell().Text(text["ui.dispatch.mode." + leg.Mode].Value);
                        table.Cell().Text(leg.Carrier ?? string.Empty);
                        table.Cell().Text(leg.From);
                        table.Cell().Text(leg.To);
                        table.Cell().Text(leg.Reference ?? string.Empty);
                        table.Cell().Text(leg.Departs is { } departs ? Time(departs, manifest.TimeZone) : string.Empty);
                        table.Cell().Text(leg.Arrives is { } arrives ? Time(arrives, manifest.TimeZone) : string.Empty);
                    }
                });

                foreach (OperationsManifestBundle bundle in manifest.Bundles)
                {
                    column.Item().PaddingTop(6).Element(item => Bundle(item, bundle, manifest.WeightUnit, manifest.DimensionUnit, text));
                }

                column.Item().PaddingTop(8).AlignRight().Text(VixlyText.Format(text, "ui.manifest.totals", new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["bundles"] = manifest.Bundles.Count,
                    ["pieces"] = manifest.Bundles.Sum(bundle => bundle.Pieces),
                    ["weight"] = Weight(manifest.Bundles.Sum(bundle => bundle.WeightKg ?? bundle.Shipments.Sum(line => line.WeightKg)), manifest.WeightUnit)
                })).SemiBold();
            });
            Footer(page, text);
        })).GeneratePdf();
    }

    public static byte[] RenderBundle(OperationsBundleManifest manifest, IStringLocalizer<OperationsText> text)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(text);

        return Document.Create(pdf => pdf.Page(page =>
        {
            Setup(page);
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(manifest.CompanyName).FontSize(14).SemiBold();
                    column.Item().Text(manifest.OriginCode + " → " + manifest.DestinationCode + " · " + manifest.ServiceCode);

                    if (manifest.DispatchNumber is { } dispatch)
                    {
                        column.Item().Text(text["ui.manifest.dispatch"].Value + ": " + dispatch);
                    }
                });
                row.ConstantItem(260).AlignRight().Text(text["ui.manifest.bundle_title"].Value).FontSize(14).SemiBold();
            });
            page.Content().PaddingVertical(10).Element(item => Bundle(item, manifest.Bundle, manifest.WeightUnit, manifest.DimensionUnit, text));
            Footer(page, text);
        })).GeneratePdf();
    }

    private static void Bundle(IContainer container, OperationsManifestBundle bundle, OperationsWeightUnit unit, OperationsDimensionUnit dimension, IStringLocalizer<OperationsText> text)
    {
        string Word(string key) => text["ui.manifest." + key].Value;
        string size = bundle.LengthCm is { } length && bundle.WidthCm is { } width && bundle.HeightCm is { } height
            ? string.Join(" x ", new[] { length, width, height }.Select(side => OperationsUnits.ToShownLength(side, dimension).ToString("0.#", CultureInfo.InvariantCulture)))
                + (dimension == OperationsDimensionUnit.Inch ? " in" : " cm")
            : string.Empty;

        container.Column(column =>
        {
            column.Item().Background("#EEF2F6").Padding(4).Text(bundle.Number + (bundle.TypeName is { } type ? " · " + type : string.Empty) + " · "
                + VixlyText.Format(text, "ui.manifest.pieces", "count", bundle.Pieces) + (bundle.WeightKg is { } kg ? " · " + Weight(kg, unit) : string.Empty)
                + (size.Length > 0 ? " · " + size : string.Empty)).SemiBold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(90);
                    columns.ConstantColumn(60);
                    columns.RelativeColumn(3);
                    columns.ConstantColumn(30);
                    columns.ConstantColumn(40);
                    columns.ConstantColumn(60);
                    columns.ConstantColumn(80);
                    columns.RelativeColumn(4);
                });
                table.Header(header =>
                {
                    foreach (string key in new[] { "shipment", "customer", "consignee", "country", "pieces_short", "weight", "value", "contents" })
                    {
                        header.Cell().BorderBottom(1).PaddingBottom(2).Text(Word(key)).SemiBold();
                    }
                });

                foreach (OperationsManifestShipment line in bundle.Shipments)
                {
                    table.Cell().PaddingVertical(1).Text(line.Number);
                    table.Cell().PaddingVertical(1).Text(line.CustomerCode);
                    table.Cell().PaddingVertical(1).Text(line.Consignee);
                    table.Cell().PaddingVertical(1).Text(line.CountryCode);
                    table.Cell().PaddingVertical(1).AlignRight().Text(line.Pieces.ToString(CultureInfo.InvariantCulture));
                    table.Cell().PaddingVertical(1).AlignRight().Text(Weight(line.WeightKg, unit));
                    table.Cell().PaddingVertical(1).AlignRight().Text(line.DeclaredValue.ToString("N2", CultureInfo.CurrentUICulture) + " " + line.CurrencyCode);
                    table.Cell().PaddingVertical(1).PaddingLeft(4).Text(line.Contents);
                }
            });
        });
    }

    private static void Setup(PageDescriptor page)
    {
        page.Size(PageSizes.Letter.Landscape());
        page.Margin(30);
        page.DefaultTextStyle(style => style.FontSize(9).FontColor("#1F2933"));
    }

    private static void Footer(PageDescriptor page, IStringLocalizer<OperationsText> text) =>
        page.Footer().AlignCenter().Text(line =>
        {
            line.Span(text["ui.manifest.page"].Value + " ").FontSize(8);
            line.CurrentPageNumber().FontSize(8);
            line.Span(" / ").FontSize(8);
            line.TotalPages().FontSize(8);
        });

    private static string Weight(decimal kilograms, OperationsWeightUnit unit) =>
        OperationsUnits.ToShownWeight(kilograms, unit).ToString("0.##", CultureInfo.InvariantCulture) + (unit == OperationsWeightUnit.Pound ? " lb" : " kg");

    private static string Time(DateTimeOffset moment, string? timeZone) =>
        OperationsTimeZones.Local(moment, timeZone).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
}
