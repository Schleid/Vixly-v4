using System.Globalization;
using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Resources;
using Vixly.Shared.Localization;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Infrastructure.Documents;

// =========================================================
// FILE: OperationsInvoiceLayout.cs
//
// PURPOSE:
// Lays out an invoice or a credit note on Letter paper (Slice 5F, I-2, I-4 and I-6).
//
// RESPONSIBILITIES:
// - The company, the customer and the reference; the lines; the taxes one per kind or in one line; the totals and what is owed; the note at the foot
// - A credit note: the lines it takes back, its taxes and its total, with the invoice it corrects
//
// IMPORTANT NOTES:
// - Words from OperationsText in the current language (the invoice's, set by the caller)
// - The company's name only, never "Vixly"
// =========================================================
internal static class OperationsInvoiceLayout
{
    public static byte[] Render(OperationsInvoiceDocument document, IStringLocalizer<OperationsText> text, Func<string, string> barcode)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(barcode);
        OperationsInvoice invoice = document.Invoice;
        OperationsCreditNote? credit = document.CreditNote;
        string Word(string key) => text["ui.invoice.pdf." + key].Value;
        string Money(decimal amount) => amount.ToString("N2", CultureInfo.CurrentUICulture) + " " + invoice.CurrencyCode;
        string number = credit?.Number ?? invoice.Number;
        DateOnly date = credit?.DecidedOn ?? invoice.IssuedOn;

        return Document.Create(pdf => pdf.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(40);
            page.DefaultTextStyle(style => style.FontSize(10).FontColor("#1F2933"));
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(invoice.Party.CompanyName).FontSize(15).SemiBold();
                    column.Item().Text(invoice.Party.WarehouseName);

                    if (invoice.Party.TaxNumber is { } taxNumber)
                    {
                        column.Item().Text(VixlyText.Format(text, "ui.invoice.pdf.tax_number", "number", taxNumber));
                    }
                });
                row.ConstantItem(200).Column(column =>
                {
                    column.Item().AlignRight().Text(Word(credit is null ? "invoice" : "credit_note")).FontSize(16).SemiBold();
                    column.Item().AlignRight().Text(number).SemiBold();
                    column.Item().AlignRight().Text(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    column.Item().AlignRight().Height(34).Svg(barcode(number)).FitArea();
                });
            });
            page.Content().PaddingVertical(14).Column(column =>
            {
                column.Spacing(10);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(to =>
                    {
                        to.Item().Text(Word("bill_to")).FontSize(9).SemiBold();
                        to.Item().Text(invoice.Party.CustomerName + (invoice.Party.CustomerCode is { } code ? " · " + code : string.Empty));

                        if (invoice.Party.CustomerCompany is { } company)
                        {
                            to.Item().Text(company);
                        }

                        to.Item().Text(invoice.Party.CustomerPhone + " · " + invoice.Party.CustomerEmail);
                    });
                    row.RelativeItem().Column(about =>
                    {
                        about.Item().Text(Word(invoice.Kind switch { OperationsInvoiceKind.Shipment => "shipment", OperationsInvoiceKind.Destination => "destination", _ => "order" })).FontSize(9).SemiBold();
                        about.Item().Text(invoice.Reference);

                        if (invoice.Route is { } route)
                        {
                            about.Item().Text(route + (invoice.ServiceCode is { } service && !text["ui.services.name." + service].ResourceNotFound ? " · " + text["ui.services.name." + service].Value : string.Empty));
                        }

                        if (credit is not null)
                        {
                            about.Item().Text(VixlyText.Format(text, "ui.invoice.pdf.corrects", "number", invoice.Number));
                        }
                    });
                });

                // Freight collect: the sender pays at the destination (owner, 2026-10-21).
                if (invoice.PayAtDestination && credit is null)
                {
                    column.Item().PaddingBottom(6).Text(Word("pay_at_destination")).SemiBold();
                }

                if (credit is null)
                {
                    Lines(column, invoice, text, Money);
                    Totals(column, invoice, text, Money);
                }
                else
                {
                    Credit(column, invoice, credit, text, Money);
                }

                if (invoice.Status == OperationsInvoiceStatus.Voided && credit is null)
                {
                    column.Item().PaddingTop(6).Text(Word("voided")).FontSize(14).SemiBold().FontColor("#B42318");
                }
            });
            page.Footer().Column(footer =>
            {
                if (invoice.Note is { } note)
                {
                    footer.Item().Text(note).FontSize(8);
                }

                footer.Item().AlignRight().Text(value =>
                {
                    value.CurrentPageNumber().FontSize(8);
                    value.Span(" / ").FontSize(8);
                    value.TotalPages().FontSize(8);
                });
            });
        })).GeneratePdf();
    }

    // A line's words: the charge's name with its parcel, or the supply's name and code.
    public static string Describe(IStringLocalizer<OperationsText> text, OperationsInvoiceLine line)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(line);

        if (line.ChargeKind is not { } kind)
        {
            return (line.Label ?? string.Empty) + (line.Code is { } code ? " (" + code + ")" : string.Empty);
        }

        string name = line.Label ?? text["ui.charges.line." + kind].Value;
        string basis = line.Basis is { } value ? value.ToString("0.###", CultureInfo.CurrentUICulture) : string.Empty;
        return kind switch
        {
            OperationsChargeLineKind.Freight => VixlyText.Format(text, "ui.charges.line.freight_of", new Dictionary<string, object> { ["piece"] = line.Piece ?? 1, ["basis"] = basis }),
            OperationsChargeLineKind.SmallPackage or OperationsChargeLineKind.Item =>
                VixlyText.Format(text, "ui.charges.line.piece_of", new Dictionary<string, object> { ["name"] = name, ["piece"] = line.Piece ?? 1 }),
            _ => name
        };
    }

    private static void Lines(ColumnDescriptor column, OperationsInvoice invoice, IStringLocalizer<OperationsText> text, Func<decimal, string> money)
    {
        bool discounts = invoice.DiscountTotal > 0m;
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.ConstantColumn(50);
                columns.ConstantColumn(80);

                if (discounts)
                {
                    columns.ConstantColumn(70);
                }

                columns.ConstantColumn(90);
            });
            table.Header(header =>
            {
                foreach (string key in discounts ? new[] { "description", "quantity", "unit_price", "discount", "amount" } : new[] { "description", "quantity", "unit_price", "amount" })
                {
                    header.Cell().BorderBottom(1).PaddingVertical(3).Text(text["ui.invoice.pdf." + key].Value).SemiBold();
                }
            });

            foreach (OperationsInvoiceLine line in invoice.Lines)
            {
                table.Cell().PaddingVertical(2).Text(Describe(text, line));
                table.Cell().PaddingVertical(2).AlignRight().Text(line.Quantity.ToString("0.###", CultureInfo.CurrentUICulture));
                table.Cell().PaddingVertical(2).AlignRight().Text(money(line.UnitPrice));

                if (discounts)
                {
                    table.Cell().PaddingVertical(2).AlignRight().Text(line.Discount > 0m ? "-" + money(line.Discount) : string.Empty);
                }

                table.Cell().PaddingVertical(2).AlignRight().Text(money(line.Amount));
            }
        });
    }

    private static void Totals(ColumnDescriptor column, OperationsInvoice invoice, IStringLocalizer<OperationsText> text, Func<decimal, string> money)
    {
        column.Item().AlignRight().Width(280).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.ConstantColumn(110);
            });

            void Row(string label, decimal amount, bool strong = false)
            {
                table.Cell().PaddingVertical(2).Text(label).SemiBold();
                IContainer cell = table.Cell().PaddingVertical(2).AlignRight();

                if (strong)
                {
                    cell.Text(money(amount)).SemiBold().FontSize(12);
                }
                else
                {
                    cell.Text(money(amount));
                }
            }

            Row(text["ui.invoice.pdf.subtotal"].Value, invoice.Subtotal);
            string taxName = invoice.TaxName ?? text["ui.invoice.pdf.tax"].Value;

            if (invoice.Taxes.Count == 0)
            {
                table.Cell().ColumnSpan(2).PaddingVertical(2).Text(text["ui.invoice.pdf.no_tax"].Value).FontSize(9);
            }
            else if (invoice.TaxPerKind)
            {
                foreach (OperationsInvoiceTax tax in invoice.Taxes)
                {
                    Row(taxName + " · " + text["ui.invoice.tax_kind." + tax.Kind].Value + " " + tax.RatePercent.ToString("0.###", CultureInfo.CurrentUICulture) + " %", tax.Amount);
                }
            }
            else
            {
                Row(taxName, invoice.TaxTotal);
            }

            Row(text["ui.invoice.pdf.total"].Value, invoice.Total, true);

            if (invoice.CreditedTotal > 0m)
            {
                Row(text["ui.invoice.pdf.credited"].Value, -invoice.CreditedTotal);
            }

            if (invoice.PaidTotal > 0m)
            {
                Row(text["ui.invoice.pdf.paid"].Value, -invoice.PaidTotal);
            }

            if (invoice.PendingTotal > 0m)
            {
                Row(text["ui.invoice.pdf.pending"].Value, -invoice.PendingTotal);
            }

            if (invoice.RefundedTotal > 0m)
            {
                Row(text["ui.invoice.pdf.refunded"].Value, invoice.RefundedTotal);
            }

            Row(text["ui.invoice.pdf.owed"].Value, Math.Max(invoice.Owed, 0m), true);
        });
    }

    private static void Credit(ColumnDescriptor column, OperationsInvoice invoice, OperationsCreditNote credit, IStringLocalizer<OperationsText> text, Func<decimal, string> money)
    {
        column.Item().Text(VixlyText.Format(text, "ui.invoice.pdf.reason", "reason", credit.Reason));
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.ConstantColumn(110);
            });
            table.Header(header =>
            {
                header.Cell().BorderBottom(1).PaddingVertical(3).Text(text["ui.invoice.pdf.description"].Value).SemiBold();
                header.Cell().BorderBottom(1).PaddingVertical(3).AlignRight().Text(text["ui.invoice.pdf.amount"].Value).SemiBold();
            });

            if (credit.Kind == OperationsCreditNoteKind.Void)
            {
                table.Cell().PaddingVertical(2).Text(VixlyText.Format(text, "ui.invoice.pdf.void_of", "number", invoice.Number));
                table.Cell().PaddingVertical(2).AlignRight().Text("-" + money(credit.Total));
            }
            else
            {
                foreach (OperationsCreditNoteLine line in credit.Lines)
                {
                    OperationsInvoiceLine? source = invoice.Lines.FirstOrDefault(item => item.Id == line.InvoiceLineId);
                    table.Cell().PaddingVertical(2).Text(source is null ? string.Empty : Describe(text, source));
                    table.Cell().PaddingVertical(2).AlignRight().Text("-" + money(line.Amount));
                }

                foreach (OperationsCreditNoteTax tax in credit.Taxes)
                {
                    table.Cell().PaddingVertical(2).Text((invoice.TaxName ?? text["ui.invoice.pdf.tax"].Value) + " · " + text["ui.invoice.tax_kind." + tax.Kind].Value);
                    table.Cell().PaddingVertical(2).AlignRight().Text("-" + money(tax.Amount));
                }
            }

            table.Cell().BorderTop(1).PaddingVertical(3).Text(text["ui.invoice.pdf.credit_total"].Value).SemiBold();
            table.Cell().BorderTop(1).PaddingVertical(3).AlignRight().Text("-" + money(credit.Total)).SemiBold().FontSize(12);
        });
    }
}
