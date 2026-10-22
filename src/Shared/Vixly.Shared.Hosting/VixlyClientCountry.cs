using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Vixly.Shared.Hosting;

// =========================================================
// FILE: VixlyClientCountry.cs
//
// PURPOSE:
// The country a request comes from, as Cloudflare names it
// (CF-IPCountry), for device registration and member activity
// (owner, 2026-10-21: "Cloudflare country").
//
// RESPONSIBILITIES:
// - Read CF-IPCountry only from the proxies trusted for the
//   client address (Hosting:TrustedProxyNetworks)
// - Keep a two-letter code, unknown (XX) and Tor (T1) included:
//   neither is a device's own country, so both count as another
//
// IMPORTANT NOTES:
// - Runs before UseForwardedHeaders, which replaces the peer
//   address with the client's
// - From anyone else the header is ignored: a visitor could
//   name any country
// =========================================================
public static partial class VixlyClientCountry
{
    private const string Key = "vixly.client_country";

    public static IApplicationBuilder UseVixlyClientCountry(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use((http, next) =>
        {
            if (Trusted(http) && Code(http.Request.Headers["CF-IPCountry"].ToString()) is { } country)
            {
                http.Items[Key] = country;
            }

            return next(http);
        });
    }

    public static string? Of(HttpContext? http) => http is not null && http.Items.TryGetValue(Key, out object? value) ? value as string : null;

    private static bool Trusted(HttpContext http)
    {
        IPAddress? peer = http.Connection.RemoteIpAddress;

        if (peer is null)
        {
            return false;
        }

        peer = peer.IsIPv4MappedToIPv6 ? peer.MapToIPv4() : peer;
        ForwardedHeadersOptions options = http.RequestServices.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
        return options.KnownProxies.Contains(peer) || options.KnownIPNetworks.Any(network => network.Contains(peer));
    }

    private static string? Code(string value)
    {
        string text = value.Trim().ToUpperInvariant();
        return CountryCode().IsMatch(text) ? text : null;
    }

    [GeneratedRegex("^[A-Z][A-Z0-9]$", RegexOptions.CultureInvariant)]
    private static partial Regex CountryCode();
}
