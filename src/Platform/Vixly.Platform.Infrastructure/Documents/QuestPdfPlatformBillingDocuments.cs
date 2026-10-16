using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Resources;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Documents;

// =========================================================
// FILE: QuestPdfPlatformBillingDocuments.cs
//
// PURPOSE:
// Renders invoices, credit notes and receipts as PDF with QuestPDF (D-B10, D-B11).
//
// RESPONSIBILITIES:
// - Lay out issuer, customer, lines, totals, payment instructions and footer
// - Write every word in the document's language (ADR-0005)
//
// IMPORTANT NOTES:
// - Called once at issue; the bytes are stored and never re-rendered
// - The logo comes from the stored company profile, never from a URL (V3 fetched any https URL)
// - Fonts: QuestPDF's bundled Lato, so output is the same on every host
// =========================================================
internal sealed class QuestPdfPlatformBillingDocuments : IPlatformBillingDocuments
{
    private static readonly string Ink = "#1F2933";

    private static readonly string Quiet = "#616E7C";

    private static readonly string Rule = "#CBD2D9";

    private readonly IStringLocalizer<PlatformText> _text;

    public QuestPdfPlatformBillingDocuments(IStringLocalizer<PlatformText> text, IOptions<PlatformDocumentOptions> options)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        string license = options?.Value.QuestPdfLicense ?? string.Empty;
        QuestPDF.Settings.License = Enum.TryParse(license, ignoreCase: true, out LicenseType type) ? type : LicenseType.Community;
    }

    public byte[] RenderInvoice(PlatformInvoice invoice, PlatformCompanyProfile issuer, PlatformBillingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return Render(invoice, issuer, settings, (text, culture) => Invoice(invoice, text, culture));
    }

    public byte[] RenderCreditNote(PlatformInvoice invoice, PlatformCreditNote credit, PlatformCompanyProfile issuer, PlatformBillingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(credit);

        return Render(invoice, issuer, settings, (text, culture) => Credit(invoice, credit, text, culture));
    }

    public byte[] RenderReceipt(PlatformInvoice invoice, PlatformPayment payment, PlatformCompanyProfile issuer, PlatformBillingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(payment);

        return Render(invoice, issuer, settings, (text, culture) => Receipt(invoice, payment, text, culture));
    }

    // =====================================================
    // METHOD: PAGE
    // =====================================================
    private byte[] Render(
        PlatformInvoice invoice,
        PlatformCompanyProfile issuer,
        PlatformBillingSettings settings,
        Func<IStringLocalizer, CultureInfo, (string Title, string Number, IReadOnlyList<(string Label, string Value)> Facts, Action<IContainer> Body)> body)
    {
        ArgumentNullException.ThrowIfNull(issuer);
        ArgumentNullException.ThrowIfNull(settings);

        CultureInfo previous = CultureInfo.CurrentUICulture;
        CultureInfo culture = CultureInfo.GetCultureInfo(invoice.Language.Value);

        try
        {
            CultureInfo.CurrentUICulture = culture;
            (string title, string number, IReadOnlyList<(string Label, string Value)> facts, Action<IContainer> content) = body(_text, culture);
            PlatformBillingText? texts = settings.TextIn(invoice.Language);

            return Document.Create(document => document.Page(page =>
            {
                page.Size(settings.PaperSize == PlatformPaperSize.Letter ? PageSizes.Letter : PageSizes.A4);
                page.Margin(42);
                page.DefaultTextStyle(style => style.FontFamily("Lato").FontSize(9.5f).FontColor(Ink));

                page.Header().PaddingBottom(18).Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        if (issuer.Logo is { } logo)
                        {
                            column.Item().Height(42).AlignLeft().Image(logo).FitHeight();
                            column.Item().PaddingTop(6);
                        }
                        else
                        {
                            column.Item().Text(issuer.Company.DisplayName).FontSize(16).SemiBold();
                        }

                        column.Item().Text(issuer.Company.LegalName).SemiBold();

                        foreach (string line in Address(issuer))
                        {
                            column.Item().Text(line).FontColor(Quiet);
                        }

                        column.Item().Text(string.Join("  ", new[] { issuer.Company.Email.Value, issuer.Company.Phone.Value, issuer.Website }.Where(part => !string.IsNullOrWhiteSpace(part)))).FontColor(Quiet);

                        if (issuer.TaxId is { } taxId)
                        {
                            column.Item().Text(VixlyText.Format(_text, "ui.billing_document.tax_id", "taxId", taxId)).FontColor(Quiet);
                        }
                    });

                    row.ConstantItem(200).AlignRight().Column(column =>
                    {
                        column.Item().AlignRight().Text(title).FontSize(20).SemiBold();
                        column.Item().AlignRight().Text(number).FontSize(11);
                        column.Item().PaddingTop(8);

                        foreach ((string label, string value) in facts)
                        {
                            column.Item().AlignRight().Text(text =>
                            {
                                text.Span(label + "  ").FontColor(Quiet);
                                text.Span(value);
                            });
                        }
                    });
                });

                page.Content().Column(column =>
                {
                    column.Spacing(14);
                    column.Item().Column(customer =>
                    {
                        customer.Item().Text(_text["ui.billing_document.bill_to"].Value).FontColor(Quiet);
                        customer.Item().Text(invoice.Customer.Name).SemiBold();

                        foreach (string line in invoice.Customer.Address.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        {
                            customer.Item().Text(line);
                        }

                        if (invoice.Customer.TaxId is { } taxId)
                        {
                            customer.Item().Text(VixlyText.Format(_text, "ui.billing_document.tax_id", "taxId", taxId));
                        }
                    });

                    column.Item().Element(content);

                    if (texts?.PaymentInstructions is { } instructions)
                    {
                        column.Item().PaddingTop(6).Column(note =>
                        {
                            note.Item().Text(_text["ui.billing_document.how_to_pay"].Value).SemiBold();
                            note.Item().Text(instructions);
                        });
                    }
                });

                page.Footer().Column(column =>
                {
                    column.Item().LineHorizontal(0.5f).LineColor(Rule);

                    if (texts?.Footer is { } footer)
                    {
                        column.Item().PaddingTop(4).Text(footer).FontSize(8).FontColor(Quiet);
                    }

                    column.Item().PaddingTop(4).AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(8).FontColor(Quiet));
                        text.Span(number + "  ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            })).GeneratePdf();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    // =====================================================
    // METHOD: INVOICE
    // =====================================================
    private (string, string, IReadOnlyList<(string, string)>, Action<IContainer>) Invoice(PlatformInvoice invoice, IStringLocalizer text, CultureInfo culture)
    {
        List<(string, string)> facts = new()
        {
            (text["ui.billing_document.issued"].Value, Date(invoice.IssuedUtc, culture)),
            (text["ui.billing_document.due"].Value, Date(invoice.DueUtc, culture))
        };

        if (invoice.PeriodStartUtc is { } from && invoice.PeriodEndUtc is { } to)
        {
            facts.Add((text["ui.billing_document.period"].Value, Date(from, culture) + " - " + Date(to, culture)));
        }

        return (text["ui.billing_document.invoice"].Value, invoice.Number, facts, container => container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Element(lines => Lines(lines, invoice, text, culture));
            column.Item().AlignRight().Width(260).Column(totals =>
            {
                Total(totals, text["ui.billing_document.subtotal"].Value, Money(invoice.NetTotal, invoice, culture));

                if (invoice.DiscountTotal > 0m)
                {
                    Total(totals, text["ui.billing_document.discount"].Value, Money(-invoice.DiscountTotal, invoice, culture));
                }

                foreach (var tax in invoice.Lines.Where(line => line.TaxRate > 0m).GroupBy(line => (line.TaxLabel, line.TaxRate)))
                {
                    Total(totals, $"{tax.Key.TaxLabel} {tax.Key.TaxRate.ToString("0.####", culture)} %", Money(tax.Sum(line => line.Tax), invoice, culture));
                }

                Total(totals, text["ui.billing_document.total"].Value, Money(invoice.Total, invoice, culture), strong: true);
            });

            if (invoice.TaxTotal == 0m)
            {
                column.Item().AlignRight().Text(text["ui.billing_document.no_tax"].Value).FontSize(8).FontColor(Quiet);
            }
        }));
    }

    private void Lines(IContainer container, PlatformInvoice invoice, IStringLocalizer text, CultureInfo culture)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(6);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(2);
                columns.RelativeColumn(1.4f);
                columns.RelativeColumn(2.2f);
            });

            table.Header(header =>
            {
                header.Cell().Element(Head).Text(text["ui.billing_document.description"].Value);
                header.Cell().Element(Head).AlignRight().Text(text["ui.billing_document.quantity"].Value);
                header.Cell().Element(Head).AlignRight().Text(text["ui.billing_document.unit_price"].Value);
                header.Cell().Element(Head).AlignRight().Text(text["ui.billing_document.tax"].Value);
                header.Cell().Element(Head).AlignRight().Text(text["ui.billing_document.amount"].Value);
            });

            foreach (PlatformInvoiceLine line in invoice.Lines.OrderBy(item => item.Position))
            {
                table.Cell().Element(Cell).Text(Describe(line.TextKey, line.Arguments, text, culture));
                table.Cell().Element(Cell).AlignRight().Text(line.Quantity.ToString("0.##", culture));
                table.Cell().Element(Cell).AlignRight().Text(line.UnitPrice.ToString("N2", culture));
                table.Cell().Element(Cell).AlignRight().Text(line.TaxRate == 0m ? "-" : line.TaxRate.ToString("0.####", culture) + " %");
                table.Cell().Element(Cell).AlignRight().Text(line.Net.ToString("N2", culture));
            }
        });
    }

    // =====================================================
    // METHOD: CREDIT NOTE
    // =====================================================
    private (string, string, IReadOnlyList<(string, string)>, Action<IContainer>) Credit(PlatformInvoice invoice, PlatformCreditNote credit, IStringLocalizer text, CultureInfo culture)
    {
        List<(string, string)> facts = new()
        {
            (text["ui.billing_document.issued"].Value, Date(credit.DecidedUtc ?? credit.CreatedUtc, culture)),
            (text["ui.billing_document.for_invoice"].Value, invoice.Number)
        };

        return (text[credit.IsVoid ? "ui.billing_document.void" : "ui.billing_document.credit_note"].Value, credit.Number ?? string.Empty, facts, container => container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text(text["ui.billing_document.reason"].Value).FontColor(Quiet);
            column.Item().Text(credit.Reason);
            column.Item().AlignRight().Width(260).Column(totals =>
            {
                Total(totals, text["ui.billing_document.invoice_total"].Value, Money(invoice.Total, invoice, culture));
                Total(totals, text["ui.billing_document.of_which_tax"].Value, Money(credit.TaxPart, invoice, culture));
                Total(totals, text["ui.billing_document.credited"].Value, Money(-credit.Amount, invoice, culture), strong: true);
            });
        }));
    }

    // =====================================================
    // METHOD: RECEIPT
    // =====================================================
    private (string, string, IReadOnlyList<(string, string)>, Action<IContainer>) Receipt(PlatformInvoice invoice, PlatformPayment payment, IStringLocalizer text, CultureInfo culture)
    {
        List<(string, string)> facts = new()
        {
            (text["ui.billing_document.received"].Value, Date(payment.ReceivedUtc, culture)),
            (text["ui.billing_document.for_invoice"].Value, invoice.Number)
        };

        return (text["ui.billing_document.receipt"].Value, payment.ReceiptNumber, facts, container => container.Column(column =>
        {
            column.Spacing(10);
            column.Item().AlignRight().Width(300).Column(totals =>
            {
                Total(totals, text["ui.billing_document.method"].Value, text["ui.payments.method." + Snake(payment.Method.ToString())].Value);

                if (payment.Reference is { } reference)
                {
                    Total(totals, text["ui.billing_document.reference"].Value, reference);
                }

                Total(totals, text["ui.billing_document.invoice_total"].Value, Money(invoice.Total, invoice, culture));
                Total(totals, text["ui.billing_document.amount_received"].Value, Money(payment.Amount, invoice, culture), strong: true);
                Total(totals, text["ui.billing_document.balance"].Value, Money(invoice.Balance, invoice, culture));
            });
        }));
    }

    // =====================================================
    // METHOD: HELPERS
    // =====================================================
    public static string Describe(string key, IReadOnlyDictionary<string, string> arguments, IStringLocalizer text, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(text);

        Dictionary<string, object> values = new(StringComparer.Ordinal);

        foreach ((string name, string value) in arguments)
        {
            values[name] = name switch
            {
                "term" => text["ui.subscriptions.term." + value.ToLowerInvariant()].Value,
                "from" or "to" when DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date) => date.ToString("d", culture),
                _ => value
            };
        }

        return VixlyText.Format(text, key, values);
    }

    private static IEnumerable<string> Address(PlatformCompanyProfile issuer)
    {
        Vixly.Platform.Domain.Tenants.PlatformPostalAddress address = issuer.Company.Address;
        yield return address.Line1;

        if (address.Line2 is { } line2)
        {
            yield return line2;
        }

        yield return string.Join(" ", new[] { address.PostalCode, address.City, address.StateProvince }.Where(part => !string.IsNullOrWhiteSpace(part)));
        yield return address.CountryCode;
    }

    private static void Total(ColumnDescriptor column, string label, string value, bool strong = false)
    {
        column.Item().BorderTop(strong ? 1 : 0).BorderColor(Rule).PaddingVertical(3).Row(row =>
        {
            TextBlockDescriptor left = row.RelativeItem().Text(label);
            TextBlockDescriptor right = row.ConstantItem(120).AlignRight().Text(value);

            if (strong)
            {
                left.SemiBold();
                right.SemiBold();
            }
        });
    }

    private static IContainer Head(IContainer container)
    {
        return container.BorderBottom(1).BorderColor(Ink).PaddingVertical(4).DefaultTextStyle(style => style.SemiBold());
    }

    private static IContainer Cell(IContainer container)
    {
        return container.BorderBottom(0.5f).BorderColor(Rule).PaddingVertical(5);
    }

    private static string Money(decimal amount, PlatformInvoice invoice, CultureInfo culture)
    {
        return amount.ToString("N2", culture) + " " + invoice.CurrencyCode;
    }

    private static string Date(DateTimeOffset value, CultureInfo culture)
    {
        return value.UtcDateTime.ToString("d", culture);
    }

    private static string Snake(string value)
    {
        return string.Concat(value.Select((character, index) => char.IsUpper(character) && index > 0 ? "_" + char.ToLowerInvariant(character) : char.ToLowerInvariant(character).ToString()));
    }
}
