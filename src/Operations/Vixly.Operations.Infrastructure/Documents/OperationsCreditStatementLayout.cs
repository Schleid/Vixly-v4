using System.Globalization;
using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Resources;
using Vixly.Shared.Localization;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Infrastructure.Documents;

// =========================================================
// FILE: OperationsCreditStatementLayout.cs
//
// PURPOSE:
// Lays out a credit account's statement on Letter paper (Slice 5F step 2b, K-7).
//
// RESPONSIBILITIES:
// - The company, the customer, the date and terms; each open item with its due date; the totals by age; the total due
//
// IMPORTANT NOTES:
// - Words from OperationsText in the current language (the customer's, set by the caller)
// =========================================================
internal static class OperationsCreditStatementLayout
{
    public static byte[] Render(OperationsCreditStatement statement, IStringLocalizer<OperationsText> text)
    {
        ArgumentNullException.ThrowIfNull(statement);
        ArgumentNullException.ThrowIfNull(text);
        string Word(string key) => text["ui.statement." + key].Value;
        string Money(decimal amount) => amount.ToString("N2", CultureInfo.CurrentUICulture) + " " + statement.CurrencyCode;
        string Day(DateOnly? day) => day?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

        return Document.Create(pdf => pdf.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(36);
            page.DefaultTextStyle(style => style.FontSize(10).FontColor("#1F2933"));
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(statement.CompanyName).FontSize(15).SemiBold();
                    column.Item().PaddingTop(6).Text(statement.Customer.FullName + (statement.Customer.Code is { } code ? " · " + code : string.Empty)).SemiBold();
                    column.Item().Text(statement.Customer.CompanyName ?? string.Empty);
                });
                row.ConstantItem(200).Column(column =>
                {
                    column.Item().AlignRight().Text(Word("title")).FontSize(15).SemiBold();
                    column.Item().AlignRight().Text(Day(statement.Day));
                    column.Item().AlignRight().Text(VixlyText.Format(text, "ui.statement.terms", "days", statement.TermsDays));
                });
            });
            page.Content().PaddingVertical(14).Column(column =>
            {
                column.Spacing(8);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(90);
                        columns.RelativeColumn();
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(80);
                    });
                    table.Header(header =>
                    {
                        foreach (string key in new[] { "invoice", "reference", "date", "due", "amount", "owed" })
                        {
                            header.Cell().BorderBottom(1).PaddingBottom(3).Text(Word(key)).SemiBold();
                        }
                    });

                    foreach (OperationsInvoice item in statement.Items)
                    {
                        table.Cell().PaddingVertical(2).Text(item.Number);
                        table.Cell().PaddingVertical(2).Text(item.Reference);
                        table.Cell().PaddingVertical(2).Text(Day(item.IssuedOn));
                        table.Cell().PaddingVertical(2).Text(Day(item.DueOn));
                        table.Cell().PaddingVertical(2).AlignRight().Text(Money(item.Total));
                        table.Cell().PaddingVertical(2).AlignRight().Text(Money(item.Owed));
                    }
                });

                if (statement.Items.Count == 0)
                {
                    column.Item().Text(Word("nothing_owed"));
                }

                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        for (int age = 0; age < 5; age++)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    for (int age = 0; age < 5; age++)
                    {
                        table.Cell().BorderBottom(1).PaddingBottom(3).AlignRight().Text(Word("age." + age)).SemiBold();
                    }

                    foreach (decimal amount in statement.Position.Ages)
                    {
                        table.Cell().PaddingTop(3).AlignRight().Text(Money(amount));
                    }
                });
                column.Item().PaddingTop(10).AlignRight().Text(VixlyText.Format(text, "ui.statement.total_due", "amount", Money(statement.Position.Owed))).FontSize(13).SemiBold();
                column.Item().AlignRight().Text(VixlyText.Format(text, "ui.statement.overdue", "amount", Money(statement.Position.Overdue)));
            });
            page.Footer().AlignCenter().Text(Word("footer")).FontSize(8);
        })).GeneratePdf();
    }
}
