using System.Globalization;
using System.Text;
using Vixly.Operations.Infrastructure.Documents;

namespace Vixly.Operations.Web.Components.Parcels;

// =========================================================
// FILE: OperationsLabelSvg.cs
//
// PURPOSE:
// Draws a parcel ID as a Code 128 barcode in SVG for labels printed from the browser (Slice 3B).
//
// RESPONSIBILITIES:
// - Write the bars as rectangles, about a quarter as tall as wide
//
// IMPORTANT NOTES:
// - Built only from the parcel number through the Code 128 tables, so it carries no markup from anyone
// =========================================================
public static class OperationsLabelSvg
{
    public static string Barcode(string parcelNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parcelNumber);
        IReadOnlyList<int> widths = OperationsCode128.Widths(parcelNumber);
        int total = widths.Sum() + 20;
        int height = Math.Max(20, total / 4);
        StringBuilder svg = new();
        svg.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {total} {height}\" preserveAspectRatio=\"none\" aria-hidden=\"true\">");
        int x = 10;

        for (int index = 0; index < widths.Count; index++)
        {
            if (index % 2 == 0)
            {
                svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"{x}\" y=\"0\" width=\"{widths[index]}\" height=\"{height}\"/>");
            }

            x += widths[index];
        }

        return svg.Append("</svg>").ToString();
    }
}
