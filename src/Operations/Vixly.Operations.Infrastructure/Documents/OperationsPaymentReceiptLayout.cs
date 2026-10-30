using System.Globalization;
using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Resources;
using Vixly.Shared.Localization;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Infrastructure.Documents;

// =========================================================
// FILE: OperationsPaymentReceiptLayout.cs
//
// PURPOSE:
// Lays out a payment receipt on half a Letter page (Slice 5F, C-3).
//
// RESPONSIBILITIES:
// - The company, the customer, the invoice paid; each tender with the cash given and the change; the total; what remains owed
//
// IMPORTANT NOTES:
// - Words from OperationsText in the current language (the invoice's, set by the caller)
// - A Zelle or transfer tender says it waits for confirmation
// =========================================================
internal static class OperationsPaymentReceiptLayout
{
    public static byte[] Render(OperationsPaymentReceipt receipt, IStringLocalizer<OperationsText> text, Func<string, string> barcode)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(barcode);
        OperationsPayment payment = receipt.Payment;
        OperationsInvoice invoice = receipt.Invoice;
        string Word(string key) => text["ui.payments.receipt." + key].Value;
        string Money(decimal amount) => amount.ToString("N2", CultureInfo.CurrentUICulture) + " " + payment.CurrencyCode;
        string Method(OperationsPaymentMethod method) => receipt.OtherNames.TryGetValue(method, out string? name) ? name : text["ui.payments.method." + method].Value;

        return Document.Create(pdf => pdf.Page(page =>
        {
            page.Size(new PageSize(PageSizes.Letter.Width, PageSizes.Letter.Height / 2));
            page.Margin(32);
            page.DefaultTextStyle(style => style.FontSize(10).FontColor("#1F2933"));
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(invoice.Party.CompanyName).FontSize(14).SemiBold();
                    column.Item().Text(invoice.Party.WarehouseName);
                });
                row.ConstantItem(190).Column(column =>
                {
                    column.Item().AlignRight().Text(Word("title")).FontSize(15).SemiBold();
                    column.Item().AlignRight().Text(payment.Number).SemiBold();
                    column.Item().AlignRight().Text(receipt.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    column.Item().AlignRight().Height(28).Svg(barcode(payment.Number)).FitArea();
                });
            });
            page.Content().PaddingVertical(10).Column(column =>
            {
                column.Spacing(6);
                column.Item().Text(invoice.Party.CustomerName + (invoice.Party.CustomerCode is { } code ? " · " + code : string.Empty));
                column.Item().Text(VixlyText.Format(text, "ui.payments.receipt.for", new Dictionary<string, object> { ["invoice"] = invoice.Number, ["reference"] = invoice.Reference }));
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(110);
                    });

                    foreach (OperationsPaymentTender tender in payment.Tenders)
                    {
                        string detail = tender.Method == OperationsPaymentMethod.Cash && tender.CashGiven is { } given
                            ? " · " + VixlyText.Format(text, "ui.payments.receipt.cash", new Dictionary<string, object> { ["given"] = Money(given), ["change"] = Money(tender.Change ?? 0m) })
                            : tender.Reference is { } reference ? " · " + reference : string.Empty;
                        string waiting = tender.Status == OperationsTenderStatus.AwaitingConfirmation ? " · " + Word("awaiting") : string.Empty;
                        table.Cell().PaddingVertical(2).Text(Method(tender.Method) + detail + waiting);
                        table.Cell().PaddingVertical(2).AlignRight().Text(Money(tender.Amount));
                    }

                    table.Cell().BorderTop(1).PaddingVertical(3).Text(Word("total")).SemiBold();
                    table.Cell().BorderTop(1).PaddingVertical(3).AlignRight().Text(Money(payment.Total)).SemiBold().FontSize(12);
                });
                column.Item().Text(VixlyText.Format(text, "ui.payments.receipt.owed", "amount", Money(Math.Max(invoice.Owed, 0m))));
            });
            page.Footer().Text(Word("thanks")).FontSize(8);
        })).GeneratePdf();
    }
}
