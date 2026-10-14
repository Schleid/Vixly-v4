using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Vixly.Shared.Hosting;

// =========================================================
// FILE: VixlyForwardedHeaders.cs
//
// PURPOSE:
// Finds the real client IP and scheme behind Cloudflare
// (ADR-0011), so rate limits and audit rows use the person's
// address, not Cloudflare's.
//
// RESPONSIBILITIES:
// - Read CF-Connecting-IP and X-Forwarded-Proto
// - Trust them only from the networks listed in
//   Hosting:TrustedProxyNetworks (Cloudflare's published
//   ranges, kept in configuration)
//
// IMPORTANT NOTES:
// - With no trusted networks configured (Development), the
//   headers are ignored and the socket address is used
// - V3 rate-limited by the Cloudflare edge address, so one
//   edge could block many users; this fixes that
// =========================================================
public static class VixlyForwardedHeaders
{
    public static IServiceCollection AddVixlyForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string[] networks = configuration.GetSection("Hosting:TrustedProxyNetworks").Get<string[]>() ?? Array.Empty<string>();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardedForHeaderName = "CF-Connecting-IP";
            options.ForwardLimit = 1;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (string network in networks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }

            if (networks.Length == 0)
            {
                options.KnownProxies.Add(IPAddress.Loopback);
                options.KnownProxies.Add(IPAddress.IPv6Loopback);
            }
        });

        return services;
    }
}
