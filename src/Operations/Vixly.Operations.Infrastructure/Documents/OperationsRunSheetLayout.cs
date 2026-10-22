using System.Globalization;
using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vixly.Operations.Application.Delivery;
using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Tracking;
using Vixly.Operations.Resources;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Infrastructure.Documents;

// =========================================================
// FILE: OperationsRunSheetLayout.cs
//
// PURPOSE:
// Lays out a delivery route's run sheet on Letter paper, landscape (Slice 7 step 3, R-7, R-12).
//
// RESPONSIBILITIES:
// - The route, its day and driver; each stop in order with its recipient, address, parcels and what to collect
// - Room for who received it and their signature, or the reason it failed (the reasons listed below)
//
// IMPORTANT NOTES:
// - Words from OperationsText in the current language
// =========================================================
internal static class OperationsRunSheetLayout
{
    public static byte[] Render(OperationsRunSheet sheet, IStringLocalizer<OperationsText> text)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(text);
        string Word(string key) => text["ui.run_sheet." + key].Value;

        return Document.Create(pdf => pdf.Page(page =>
        {
            page.Size(PageSizes.Letter.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(style => style.FontSize(9).FontColor("#1F2933"));
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(sheet.CompanyName).FontSize(14).SemiBold();
                    column.Item().Text(sheet.WarehouseName + " · " + sheet.ServiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    column.Item().Text(Word("driver") + ": " + sheet.DriverName);
                });
                row.ConstantItem(260).Column(column =>
                {
                    column.Item().AlignRight().Text(Word("title")).FontSize(14).SemiBold();
                    column.Item().AlignRight().Text(sheet.Number).FontSize(12);
                });
            });
            page.Content().PaddingVertical(10).Column(column =>
            {
                column.Spacing(6);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(22);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(4);
                        columns.ConstantColumn(40);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(3);
                    });
                    table.Header(header =>
                    {
                        foreach (string key in new[] { "stop", "shipment", "recipient", "address", "parcels", "collect", "received_by", "failed" })
                        {
                            header.Cell().BorderBottom(1).PaddingBottom(2).Text(Word(key)).SemiBold();
                        }
                    });

                    foreach (OperationsDeliveryStopRow stop in sheet.Stops)
                    {
                        table.Cell().BorderBottom(0.5f).PaddingVertical(8).Text(stop.Sequence.ToString(CultureInfo.InvariantCulture));
                        table.Cell().BorderBottom(0.5f).PaddingVertical(8).Text(stop.Number);
                        table.Cell().BorderBottom(0.5f).PaddingVertical(8).Text((stop.RecipientName ?? string.Empty) + (stop.RecipientPhone is { } phone ? "\n" + phone : string.Empty));
                        table.Cell().BorderBottom(0.5f).PaddingVertical(8).Text(stop.AddressLine);
                        table.Cell().BorderBottom(0.5f).PaddingVertical(8).Text(stop.Parcels.ToString(CultureInfo.InvariantCulture));
                        table.Cell().BorderBottom(0.5f).PaddingVertical(8).Text(stop.Due > 0m ? stop.Due.ToString("0.00", CultureInfo.InvariantCulture) + " " + (stop.CurrencyCode ?? sheet.CurrencyCode)
                            : Word("nothing"));
                        table.Cell().BorderBottom(0.5f).PaddingVertical(8).Text(string.Empty);
                        table.Cell().BorderBottom(0.5f).PaddingVertical(8).Text(string.Empty);
                    }
                });

                List<string> asked = new() { Word("proof.name") };

                foreach (OperationsDeliveryProofs proof in new[] { OperationsDeliveryProofs.Signature, OperationsDeliveryProofs.Id })
                {
                    if (sheet.Proofs.HasFlag(proof))
                    {
                        asked.Add(Word("proof." + proof.ToString().ToLowerInvariant()));
                    }
                }

                column.Item().PaddingTop(8).Text(Word("proof") + ": " + string.Join(", ", asked));
                column.Item().Text(Word("reasons") + ": " + string.Join(" · ", OperationsTrackingCodes.DeliveryFailures.Select(reason => text["ui.delivery.failure." + reason].Value)));
                column.Item().PaddingTop(16).Row(row =>
                {
                    row.RelativeItem().Text(Word("cash_handed_in") + ": ____________________");
                    row.RelativeItem().Text(Word("driver_signature") + ": ____________________");
                });
            });
            page.Footer().AlignCenter().Text(line =>
            {
                line.Span(text["ui.manifest.page"].Value + " ").FontSize(8);
                line.CurrentPageNumber().FontSize(8);
                line.Span(" / ").FontSize(8);
                line.TotalPages().FontSize(8);
            });
        })).GeneratePdf();
    }
}
