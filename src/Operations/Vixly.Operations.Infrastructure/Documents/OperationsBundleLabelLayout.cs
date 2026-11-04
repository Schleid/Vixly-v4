using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Resources;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Infrastructure.Documents;

// =========================================================
// FILE: OperationsBundleLabelLayout.cs
//
// PURPOSE:
// Lays out the 4 x 6 labels of a bundle and a repack box (Slice 6A, D-2 and D-4; owner, A-6).
//
// RESPONSIBILITIES:
// - The route large, the number as a barcode, the service, the type, the parcels and, once closed, the weight
//
// IMPORTANT NOTES:
// - Words from OperationsText in the current language; black on white for thermal printers
// =========================================================
internal static class OperationsBundleLabelLayout
{
    public static byte[] Render(OperationsBundleLabel label, IStringLocalizer<OperationsText> text, Func<string, string> barcode)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(barcode);
        string code = barcode(label.Number);

        return Document.Create(document => document.Page(page =>
        {
            page.Size(new PageSize(4 * 72, 6 * 72));
            page.Margin(14);
            page.DefaultTextStyle(style => style.FontSize(10).FontColor("#000000"));
            page.Content().Column(column =>
            {
                column.Spacing(6);
                column.Item().Text(label.CompanyName).FontSize(10).SemiBold();
                column.Item().Text(label.OriginCode + " > " + label.DestinationCode).FontSize(34).Bold();
                column.Item().LineHorizontal(1.5f);
                column.Item().Text(label.ServiceCode.Replace('_', ' ').ToUpperInvariant() + (label.TypeName is { } type ? " · " + type : string.Empty)).FontSize(14).SemiBold();
                column.Item().Text(VixlyText.Format(text, "ui.bundles.label.parcels", "count", label.Parcels)).FontSize(13);
                column.Item().Text(label.Closed && label.Weight is { } weight ? weight + (label.Size is { } size ? " · " + size : string.Empty) : text["ui.bundles.label.open"].Value).FontSize(13);
                column.Item().PaddingTop(8).Height(90).Svg(code).FitArea();
                column.Item().Text(label.Number).FontSize(16).Bold();
            });
        })).GeneratePdf();
    }

    public static byte[] Render(OperationsRepackBoxLabel label, IStringLocalizer<OperationsText> text, Func<string, string> barcode)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(barcode);
        string code = barcode(label.Number);
        string? shipment = label.ShipmentNumber is { } number ? barcode(number) : null;

        return Document.Create(document => document.Page(page =>
        {
            page.Size(new PageSize(4 * 72, 6 * 72));
            page.Margin(14);
            page.DefaultTextStyle(style => style.FontSize(10).FontColor("#000000"));
            page.Content().Column(column =>
            {
                column.Spacing(6);
                column.Item().Text(label.CompanyName).FontSize(10).SemiBold();
                column.Item().Text(label.OriginCode + " > " + label.DestinationCode).FontSize(34).Bold();
                column.Item().LineHorizontal(1.5f);
                column.Item().Text(label.RecipientName).FontSize(14).SemiBold();
                column.Item().Text((label.ServiceCode?.Replace('_', ' ').ToUpperInvariant() ?? "-") + "   " + (label.Weight ?? string.Empty)).FontSize(11);
                column.Item().Text(VixlyText.Format(text, "ui.bundles.label.repacked", "count", label.Parcels)).FontSize(13);
                column.Item().PaddingTop(4).Height(70).Svg(code).FitArea();
                column.Item().Text(label.Number).FontSize(12).Bold();

                if (shipment is not null)
                {
                    column.Item().PaddingTop(4).Height(45).Svg(shipment).FitArea();
                    column.Item().Text(label.ShipmentNumber!).FontSize(10);
                }
            });
        })).GeneratePdf();
    }
}
