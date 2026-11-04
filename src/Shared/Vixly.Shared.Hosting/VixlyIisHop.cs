using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Vixly.Shared.Hosting;

// =========================================================
// FILE: VixlyIisHop.cs
//
// PURPOSE:
// Undoes IIS's own hop when a site runs out of process, so the
// rest of the pipeline sees the request as IIS received it, as
// it does in process (production, 2026-10-24: the customer
// portal shares Operations Web's pool, so both run out of process).
//
// RESPONSIBILITIES:
// - Only behind IIS out of process, and only for a request from
//   the loopback address (IIS itself): take IIS's peer from the
//   last X-Forwarded-For value and IIS's scheme from the last
//   X-Forwarded-Proto value, then remove those two values
// - Once per request
//
// IMPORTANT NOTES:
// - Out of process, IIS sets ASPNETCORE_TOKEN and the app listens
//   on loopback only; the IIS integration refuses requests without
//   IIS's token, so a loopback peer here is IIS
// - Hosting:IisOutOfProcess (true/false) overrides the detection
//   (tests)
// - The Cloudflare rules that follow are unchanged: the peer is
//   again Cloudflare's address (or a direct visitor's), and only
//   Cloudflare is trusted for CF-Connecting-IP and CF-IPCountry
// - Without this the app saw every request as http from loopback:
//   the https redirect looped and every visitor shared one address
// =========================================================
public sealed class VixlyIisHop
{
    private const string Done = "vixly.iis_hop";

    private const string ForwardedFor = "X-Forwarded-For";

    private const string ForwardedProto = "X-Forwarded-Proto";

    private VixlyIisHop(bool enabled)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }

    public static VixlyIisHop From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        bool enabled = bool.TryParse(configuration["Hosting:IisOutOfProcess"], out bool chosen)
            ? chosen
            : !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_TOKEN"));
        return new VixlyIisHop(enabled);
    }

    public void Apply(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);

        if (!Enabled || !http.Items.TryAdd(Done, true))
        {
            return;
        }

        IPAddress? peer = http.Connection.RemoteIpAddress;

        if (peer is null || !IPAddress.IsLoopback(peer.IsIPv4MappedToIPv6 ? peer.MapToIPv4() : peer))
        {
            return;
        }

        if (TakeLast(http.Request.Headers, ForwardedFor) is { } from && Endpoint(from) is { } endpoint)
        {
            http.Connection.RemoteIpAddress = endpoint.Address;
            http.Connection.RemotePort = endpoint.Port;
        }

        if (TakeLast(http.Request.Headers, ForwardedProto) is { } scheme
            && (scheme.Equals("https", StringComparison.OrdinalIgnoreCase) || scheme.Equals("http", StringComparison.OrdinalIgnoreCase)))
        {
            http.Request.Scheme = scheme.ToLowerInvariant();
        }
    }

    // The last comma-separated value, removed from the header (the header goes when it was the only one).
    private static string? TakeLast(IHeaderDictionary headers, string name)
    {
        List<string> values = headers[name].SelectMany(value => (value ?? string.Empty).Split(','))
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .ToList();

        if (values.Count == 0)
        {
            return null;
        }

        string last = values[^1];
        values.RemoveAt(values.Count - 1);

        if (values.Count == 0)
        {
            headers.Remove(name);
        }
        else
        {
            headers[name] = new StringValues(string.Join(", ", values));
        }

        return last;
    }

    // IIS sends "address:port" ("[address]:port" for IPv6); a bare address is accepted too.
    private static IPEndPoint? Endpoint(string value)
    {
        if (IPEndPoint.TryParse(value, out IPEndPoint? endpoint))
        {
            return endpoint;
        }

        return IPAddress.TryParse(value, out IPAddress? address) ? new IPEndPoint(address, 0) : null;
    }
}
