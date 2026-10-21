using System.Text.RegularExpressions;

namespace Vixly.Operations.Domain.Receiving;

// =========================================================
// FILE: OperationsCarrierBarcode.cs
//
// PURPOSE:
// Reads the tracking number out of what a scanner returns from a carrier label (Slice 3B).
//
// RESPONSIBILITIES:
// - Clean the scan and give the tracking numbers it may stand for, the most likely first
// - Name the carrier when the pattern is certain
//
// IMPORTANT NOTES:
// - USPS labels encode "420" and the ZIP code before the tracking number; FedEx 1D barcodes carry the tracking number in their last 12 (Express) or 15 (Ground) digits
// - Customers type the number printed under the barcode; matching tries every candidate, so either form finds the pre-alert
// - A USPS routing barcode (420 and the ZIP code alone) is not a tracking number (review of the dock)
// =========================================================
public static class OperationsCarrierBarcode
{
    private static readonly Regex UspsWithZip = new("^420(?:\\d{5}|\\d{9})(?<tracking>9\\d{19,33})$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex Ups = new("^1Z[0-9A-Z]{16}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex Amazon = new("^TBA\\d{9,15}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex Usps = new("^9[2-5]\\d{18,32}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ZipRouting = new("^420\\d{5}(?:\\d{4})?$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    // What the scanner sent without its AIM prefix (such as ]C1), separators and spaces, in upper case.
    public static string Clean(string? scanned)
    {
        string text = (scanned ?? string.Empty).Trim();

        if (text.StartsWith(']') && text.Length > 3)
        {
            text = text[3..];
        }

        return new string(text.Where(char.IsAsciiLetterOrDigit).Take(60).ToArray()).ToUpperInvariant();
    }

    // The tracking numbers the scan may stand for, the most likely first; empty when nothing usable was read.
    public static IReadOnlyList<string> Candidates(string? scanned)
    {
        // Scanners may send the GS1 group separator or an AIM prefix such as ]C1 before the data.
        string text = Clean(scanned);
        List<string> found = new();

        if (ZipRouting.IsMatch(text))
        {
            return found;
        }

        if (UspsWithZip.Match(text) is { Success: true } usps)
        {
            found.Add(usps.Groups["tracking"].Value);
        }

        if (text.Length == 34 && text.All(char.IsAsciiDigit))
        {
            found.Add(text[^12..]);
            found.Add(text[^15..]);
        }

        if (text.Length == 22 && text.StartsWith("96", StringComparison.Ordinal))
        {
            found.Add(text[^15..]);
        }

        found.Add(text);
        return found.Where(item => OperationsCarrierTracking.TryNormalize(item, out _)).Distinct(StringComparer.Ordinal).ToList();
    }

    public static string? CarrierOf(string trackingNumber)
    {
        ArgumentNullException.ThrowIfNull(trackingNumber);

        return trackingNumber switch
        {
            _ when Ups.IsMatch(trackingNumber) => "UPS",
            _ when Amazon.IsMatch(trackingNumber) => "Amazon",
            _ when Usps.IsMatch(trackingNumber) => "USPS",
            _ when trackingNumber.Length is 12 or 15 && trackingNumber.All(char.IsAsciiDigit) => "FedEx",
            _ => null
        };
    }
}
