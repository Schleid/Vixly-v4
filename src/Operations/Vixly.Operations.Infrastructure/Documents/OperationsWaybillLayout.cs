using System.Globalization;
using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vixly.Operations.Application.Dispatch;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Resources;

namespace Vixly.Operations.Infrastructure.Documents;

// =========================================================
// FILE: OperationsWaybillLayout.cs
//
// PURPOSE:
// Lays out house air waybills and the Shipper's Letter of Instruction on Letter paper (Slice 6B step 2, D-6).
//
// RESPONSIBILITIES:
// - House air waybill: issuing agent, master and house numbers, shipper and consignee, airports and routing, pieces, weights, goods, values, charges, handling, issue and signature
// - Letter of instruction: shipper, consignee, carrier and account, airports, master air waybill, pieces, weights, goods, value, charges, export reference, documents, instructions, signature
//
// IMPORTANT NOTES:
// - Boxed as the standard forms are laid out; words from OperationsText in the current language
// - The company's name only, never "Vixly"; declared value for carriage is "NVD"
// =========================================================
internal static class OperationsWaybillLayout
{
    public static byte[] RenderHouseWaybills(IReadOnlyList<OperationsHouseWaybill> waybills, IStringLocalizer<OperationsText> text)
    {
        ArgumentNullException.ThrowIfNull(waybills);
        ArgumentNullException.ThrowIfNull(text);
        string Word(string key) => text["ui.waybill." + key].Value;

        return Document.Create(pdf =>
        {
            foreach (OperationsHouseWaybill waybill in waybills)
            {
                pdf.Page(page =>
                {
                    Setup(page);
                    page.Content().Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(head =>
                            {
                                head.Item().Text(Word("house_title")).FontSize(14).SemiBold();
                                head.Item().Text(Word("master") + ": " + (waybill.MasterWaybill ?? "—"));
                            });
                            row.ConstantItem(220).AlignRight().Column(head =>
                            {
                                head.Item().AlignRight().Text(Word("house")).FontSize(8);
                                head.Item().AlignRight().Text(waybill.HouseNumber).FontSize(14).SemiBold();
                            });
                        });
                        column.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Element(box => Party(box, Word("shipper"), waybill.Shipper));
                            row.RelativeItem().Element(box => Party(box, Word("agent"), waybill.Agent));
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(box => Party(box, Word("consignee"), waybill.Consignee));
                            row.RelativeItem().Element(box => Box(box, Word("routing"), string.Join("\n", waybill.Routing)));
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(box => Box(box, Word("departure"), waybill.Departure));
                            row.RelativeItem().Element(box => Box(box, Word("destination"), waybill.Destination));
                            row.RelativeItem().Element(box => Box(box, Word("charges"), Word(waybill.Collect ? "collect" : "prepaid")));
                        });
                        column.Item().Row(row =>
                        {
                            row.ConstantItem(70).Element(box => Box(box, Word("pieces"), waybill.Pieces.ToString(CultureInfo.InvariantCulture)));
                            row.ConstantItem(110).Element(box => Box(box, Word("gross"), Weight(waybill.WeightKg, waybill.WeightUnit)));
                            row.ConstantItem(110).Element(box => Box(box, Word("chargeable"), Weight(waybill.ChargeableKg, waybill.WeightUnit)));
                            row.RelativeItem().Element(box => Box(box, Word("goods"), waybill.Contents));
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(box => Box(box, Word("value_carriage"), "NVD"));
                            row.RelativeItem().Element(box => Box(box, Word("value_customs"), waybill.DeclaredValue.ToString("N2", CultureInfo.CurrentUICulture) + " " + waybill.CurrencyCode));
                        });
                        column.Item().Element(box => Box(box, Word("handling"), waybill.Handling ?? string.Empty));
                        column.Item().Element(box => Box(box, Word("terms"), Word("terms_text")));
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(box => Box(box, Word("issued"), waybill.IssuedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " · " + waybill.IssuedAt));
                            row.RelativeItem().Element(box => Box(box, Word("signature_agent"), waybill.CompanyName + "\n\n"));
                        });
                    });
                });
            }
        }).GeneratePdf();
    }

    public static byte[] RenderShipperLetter(OperationsShipperLetter letter, IStringLocalizer<OperationsText> text)
    {
        ArgumentNullException.ThrowIfNull(letter);
        ArgumentNullException.ThrowIfNull(text);
        string Word(string key) => text["ui.sli." + key].Value;

        return Document.Create(pdf => pdf.Page(page =>
        {
            Setup(page);
            page.Content().Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(Word("title")).FontSize(14).SemiBold();
                    row.ConstantItem(220).AlignRight().Text(letter.DispatchNumber).FontSize(12).SemiBold();
                });
                column.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Element(box => Party(box, Word("shipper"), letter.Shipper));
                    row.RelativeItem().Element(box => Party(box, Word("consignee"), letter.Consignee));
                });
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(box => Box(box, Word("carrier"), letter.Carrier ?? string.Empty));
                    row.RelativeItem().Element(box => Box(box, Word("account"), letter.AccountNumber ?? string.Empty));
                    row.RelativeItem().Element(box => Box(box, Word("master"), letter.MasterWaybill ?? string.Empty));
                });
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(box => Box(box, Word("departure"), letter.Departure));
                    row.RelativeItem().Element(box => Box(box, Word("destination"), letter.Destination));
                    row.RelativeItem().Element(box => Box(box, Word("charges"), Word(letter.Collect ? "collect" : "prepaid")));
                });
                column.Item().Row(row =>
                {
                    row.ConstantItem(70).Element(box => Box(box, Word("pieces"), letter.Pieces.ToString(CultureInfo.InvariantCulture)));
                    row.ConstantItem(110).Element(box => Box(box, Word("gross"), Weight(letter.WeightKg, letter.WeightUnit)));
                    row.ConstantItem(110).Element(box => Box(box, Word("chargeable"), Weight(letter.ChargeableKg, letter.WeightUnit)));
                    row.RelativeItem().Element(box => Box(box, Word("goods"), Word("consolidation")));
                });
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(box => Box(box, Word("value_carriage"), "NVD"));
                    row.RelativeItem().Element(box => Box(box, Word("value_customs"), string.Join(" + ", letter.DeclaredValues.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                        .Select(pair => pair.Value.ToString("N2", CultureInfo.CurrentUICulture) + " " + pair.Key))));
                    row.RelativeItem().Element(box => Box(box, Word("export"), letter.ExportReference ?? Word("export_none")));
                });
                column.Item().Element(box => Box(box, Word("documents"), string.Join("\n", Enum.GetValues<OperationsDispatchDocuments>()
                    .Where(item => item != OperationsDispatchDocuments.None)
                    .Select(item => (letter.Documents.HasFlag(item) ? "[x] " : "[ ] ") + text["ui.dispatch.document." + item].Value))));
                column.Item().Element(box => Box(box, Word("handling"), letter.Handling ?? string.Empty));
                column.Item().Element(box => Box(box, Word("authority"), Word("authority_text")));
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(box => Box(box, Word("date"), letter.IssuedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    row.RelativeItem().Element(box => Box(box, Word("signature"), letter.CompanyName + "\n\n"));
                });
            });
        })).GeneratePdf();
    }

    private static void Setup(PageDescriptor page)
    {
        page.Size(PageSizes.Letter);
        page.Margin(30);
        page.DefaultTextStyle(style => style.FontSize(9).FontColor("#1F2933"));
    }

    private static void Party(IContainer container, string label, OperationsWaybillParty party) =>
        Box(container, label, string.Join("\n", new[] { party.Name }.Concat(party.Lines).Concat(party.Phone is { } phone ? new[] { phone } : Array.Empty<string>())));

    private static void Box(IContainer container, string label, string value) =>
        container.Border(0.75f).BorderColor("#52606D").Padding(4).MinHeight(34).Column(column =>
        {
            column.Item().Text(label).FontSize(7).FontColor("#52606D");
            column.Item().Text(value);
        });

    private static string Weight(decimal kilograms, OperationsWeightUnit unit) =>
        OperationsUnits.ToShownWeight(kilograms, unit).ToString("0.##", CultureInfo.InvariantCulture) + (unit == OperationsWeightUnit.Pound ? " lb" : " kg");
}
