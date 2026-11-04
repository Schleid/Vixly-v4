using System.Globalization;
using System.Text.RegularExpressions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsColour.cs
//
// PURPOSE:
// A #RRGGBB colour and WCAG contrast (Slice 1B).
//
// RESPONSIBILITIES:
// - Normalize a hex colour
// - Compute relative luminance and contrast ratio (WCAG 2.2)
//
// IMPORTANT NOTES:
// - Only 6-digit hex: no names, no alpha, nothing that could escape a style
// =========================================================
public static class OperationsColour
{
    public const string White = "#FFFFFF";

    private static readonly Regex Hex = new("^#[0-9A-F]{6}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Normalize(string? value)
    {
        string colour = (value ?? string.Empty).Trim().ToUpperInvariant();
        return Hex.IsMatch(colour) ? colour : throw new DomainException(OperationsBrandingErrors.ColourInvalid);
    }

    public static double Contrast(string first, string second)
    {
        double a = Luminance(first);
        double b = Luminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static double Luminance(string colour)
    {
        string hex = Normalize(colour);
        return (0.2126 * Channel(hex, 1)) + (0.7152 * Channel(hex, 3)) + (0.0722 * Channel(hex, 5));
    }

    private static double Channel(string hex, int index)
    {
        double value = int.Parse(hex.AsSpan(index, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
