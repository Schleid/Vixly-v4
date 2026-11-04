using System.Globalization;
using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Resources;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Infrastructure.Documents;

// =========================================================
// FILE: OperationsQuoteLayout.cs
//
// PURPOSE:
// Lays out a quote on Letter paper (owner, 2026-10-28: staff print it or email it to the customer).
//
// RESPONSIBILITIES:
// - The company's logo, the quote's number, date and "valid until"; the customer and the route
// - The pieces as quoted, the priced lines and the total; how long the price holds
//
// IMPORTANT NOTES:
// - Words from OperationsText in the current language (the customer's, set by the caller)
// - The company's name and logo only, never "Vixly"
// =========================================================
internal static class OperationsQuoteLayout
{
    public static byte[] Render(OperationsQuoteDocument document, IStringLocalizer<OperationsText> text, Func<string, string> barcode, byte[]? logo)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(barcode);
        OperationsQuote quote = document.Quote;
        string Word(string key) => text["ui.quote.pdf." + key].Value;
        string Money(decimal amount) => amount.ToString("N2", CultureInfo.CurrentUICulture) + " " + quote.CurrencyCode;
        string Day(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string weight = document.WeightUnit == OperationsWeightUnit.Pound ? "lb" : "kg";
        string length = document.DimensionUnit == OperationsDimensionUnit.Inch ? "in" : "cm";
        string Number(decimal value) => value.ToString("0.##", CultureInfo.CurrentUICulture);

        return Document.Create(pdf => pdf.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(40);
            page.DefaultTextStyle(style => style.FontSize(10).FontColor("#1F2933"));
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(column => OperationsDocumentBrand.Head(column, logo, document.CompanyName, 15));
                row.ConstantItem(200).Column(column =>
                {
                    column.Item().AlignRight().Text(Word("title")).FontSize(16).SemiBold();
                    column.Item().AlignRight().Text(quote.Number).SemiBold();
                    column.Item().AlignRight().Text(Day(document.IssuedOn));
                    column.Item().AlignRight().Height(30).Svg(barcode(quote.Number)).FitArea();
                });
            });
            page.Content().PaddingVertical(14).Column(column =>
            {
                column.Spacing(10);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(to =>
                    {
                        to.Item().Text(Word("for")).FontSize(9).SemiBold();
                        to.Item().Text(document.CustomerName + (document.CustomerCode is { } code ? " · " + code : string.Empty));
                        to.Item().Text(document.CustomerEmail);
                    });
                    row.RelativeItem().Column(about =>
                    {
                        about.Item().Text(Word("route")).FontSize(9).SemiBold();
                        about.Item().Text((document.Origin ?? "—") + " → " + (document.Destination ?? quote.CountryCode ?? "—"));
                        string service = text["ui.services.name." + quote.ServiceCode] is { ResourceNotFound: false } named ? named.Value : quote.ServiceCode;
                        List<string> options = new() { service };

                        if (quote.Consolidated)
                        {
                            options.Add(Word("consolidated"));
                        }

                        if (quote.Insured)
                        {
                            options.Add(Word("insured"));
                        }

                        about.Item().Text(string.Join(" · ", options));
                    });
                });

                column.Item().Background("#F1F5F9").Padding(6).Text(VixlyText.Format(text, "ui.quote.pdf.valid_until", "date", Day(document.ValidUntil))).SemiBold();

                // The pieces as quoted, in the company's units.
                column.Item().Text(Word("pieces")).FontSize(11).SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(28);
                        columns.RelativeColumn();
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(110);
                        columns.ConstantColumn(90);
                    });
                    table.Header(header =>
                    {
                        foreach (string key in new[] { "piece", "contents", "weight", "size", "value" })
                        {
                            header.Cell().BorderBottom(1).PaddingVertical(3).Text(Word(key)).SemiBold();
                        }
                    });
                    int index = 0;

                    foreach (OperationsQuotePiece piece in quote.Pieces)
                    {
                        index++;
                        table.Cell().PaddingVertical(2).Text(index.ToString(CultureInfo.InvariantCulture));
                        table.Cell().PaddingVertical(2).Text(string.Join(" · ", new[] { piece.ItemName, piece.Contents }.Where(part => !string.IsNullOrWhiteSpace(part))));
                        table.Cell().PaddingVertical(2).AlignRight().Text(Number(OperationsUnits.ToShownWeight(piece.WeightKg, document.WeightUnit)) + " " + weight);
                        table.Cell().PaddingVertical(2).AlignRight().Text(string.Join(" × ", new[] { piece.LengthCm, piece.WidthCm, piece.HeightCm }
                            .Select(size => Number(OperationsUnits.ToShownLength(size, document.DimensionUnit)))) + " " + length);
                        table.Cell().PaddingVertical(2).AlignRight().Text(Money(piece.DeclaredValue));
                    }
                });

                // The priced lines and the total.
                column.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(110);
                    });
                    table.Header(header =>
                    {
                        header.Cell().BorderBottom(1).PaddingVertical(3).Text(Word("description")).SemiBold();
                        header.Cell().BorderBottom(1).PaddingVertical(3).AlignRight().Text(Word("amount")).SemiBold();
                    });

                    foreach (OperationsQuoteLine line in quote.Lines)
                    {
                        table.Cell().PaddingVertical(2).Text(Describe(text, line) + (line.IsGovernmentFee ? " (" + text["ui.charges.extra.government_short"].Value + ")" : string.Empty));
                        table.Cell().PaddingVertical(2).AlignRight().Text(Money(line.Amount));
                    }

                    table.Cell().BorderTop(1).PaddingVertical(4).Text(Word("total")).FontSize(12).SemiBold();
                    table.Cell().BorderTop(1).PaddingVertical(4).AlignRight().Text(Money(quote.Total)).FontSize(12).SemiBold();
                });
            });
            page.Footer().Column(footer =>
            {
                footer.Item().Text(VixlyText.Format(text, "ui.quote.pdf.terms", "date", Day(document.ValidUntil))).FontSize(8);
                footer.Item().AlignRight().Text(value =>
                {
                    value.CurrentPageNumber().FontSize(8);
                    value.Span(" / ").FontSize(8);
                    value.TotalPages().FontSize(8);
                });
            });
        })).GeneratePdf();
    }

    // As on the quote page: the freight of each piece, a piece's item, an extra and its quantity.
    private static string Describe(IStringLocalizer<OperationsText> text, OperationsQuoteLine line)
    {
        string name = line.Label ?? text["ui.charges.line." + line.Kind].Value;
        string basis = line.Basis is { } value ? value.ToString("0.###", CultureInfo.CurrentUICulture) : string.Empty;
        return line.Kind switch
        {
            OperationsChargeLineKind.Freight => VixlyText.Format(text, "ui.charges.line.freight_of", new Dictionary<string, object> { ["piece"] = line.Piece ?? 1, ["basis"] = basis }),
            OperationsChargeLineKind.SmallPackage or OperationsChargeLineKind.Item =>
                VixlyText.Format(text, "ui.charges.line.piece_of", new Dictionary<string, object> { ["name"] = name, ["piece"] = line.Piece ?? 1 }),
            OperationsChargeLineKind.Extra when line.Quantity > 1 => name + " × " + line.Quantity.ToString("0.##", CultureInfo.CurrentUICulture),
            _ => name
        };
    }
}
