namespace Vixly.PrintHelper.Core;

// =========================================================
// FILE: PairingLink.cs
//
// PURPOSE:
// The link in the QR code of the Printers page (Slice 12E, owner H-6): the company's address and the one-time code.
//
// RESPONSIBILITIES:
// - Read a scanned link, or a typed address and code
//
// IMPORTANT NOTES:
// - Only https addresses; the code is checked by Operations
// =========================================================
public sealed record PairingLink(Uri Site, string Code)
{
    // https://{company}/agent/v1/pair?code=XXXX-XXXX-XXXX
    public static PairingLink? FromScan(string? scanned)
    {
        if (!Uri.TryCreate(scanned?.Trim(), UriKind.Absolute, out Uri? link) || link.Scheme != Uri.UriSchemeHttps
            || !link.AbsolutePath.Equals("/agent/v1/pair", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string? code = link.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(pair => pair.Length == 2 && pair[0] == "code")
            .Select(pair => Uri.UnescapeDataString(pair[1]))
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(code) ? null : new PairingLink(new Uri(link.GetLeftPart(UriPartial.Authority) + "/"), code.Trim());
    }

    // The company's address typed by hand (with or without https://) and the code as shown under the QR code.
    public static PairingLink? FromTyped(string? address, string? code)
    {
        string host = (address ?? string.Empty).Trim();
        host = host.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? host : "https://" + host;
        return string.IsNullOrWhiteSpace(code) || !Uri.TryCreate(host, UriKind.Absolute, out Uri? site) || site.Scheme != Uri.UriSchemeHttps || site.Host.Length == 0
            ? null
            : new PairingLink(new Uri(site.GetLeftPart(UriPartial.Authority) + "/"), code.Trim());
    }
}
