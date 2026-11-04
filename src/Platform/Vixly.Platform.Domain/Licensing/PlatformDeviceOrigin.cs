namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformDeviceOrigin.cs
//
// PURPOSE:
// Where a device was registered from: the address, the browser's
// user agent and the country (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Keep each value within its column; drop what is not usable
//
// IMPORTANT NOTES:
// - As the tenant's server saw the person (Cloudflare's address
//   and country); a hint for review, not a proof
// - Unknown (XX) and Tor (T1) are never a device's country
// =========================================================
public sealed record PlatformDeviceOrigin
{
    public PlatformDeviceOrigin(string? ip, string? userAgent, string? country)
    {
        Ip = Clip(ip, PlatformLicensingLimits.IpMaxLength);
        UserAgent = Clip(userAgent, PlatformLicensingLimits.UserAgentMaxLength);
        string? code = country?.Trim().ToUpperInvariant();
        Country = code is { Length: 2 } && code.All(char.IsAsciiLetterOrDigit) && code is not ("XX" or "T1") ? code : null;
    }

    public static PlatformDeviceOrigin None { get; } = new(null, null, null);

    public string? Ip { get; }

    public string? UserAgent { get; }

    public string? Country { get; }

    private static string? Clip(string? value, int length)
    {
        string? text = value?.Trim();
        return string.IsNullOrEmpty(text) ? null : text.Length > length ? text[..length] : text;
    }
}
