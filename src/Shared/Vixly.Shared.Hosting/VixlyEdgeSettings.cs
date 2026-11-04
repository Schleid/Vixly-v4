using Microsoft.Extensions.Configuration;

namespace Vixly.Shared.Hosting;

// =========================================================
// FILE: VixlyEdgeSettings.cs
//
// PURPOSE:
// What the Cloudflare Worker of the side-by-side rollout may
// forward: the company addresses it serves and the secret it
// proves itself with.
//
// RESPONSIBILITIES:
// - Read Hosting:EdgeHosts (for example vixly.ltd and
//   *.vixly.ltd) and Hosting:EdgeSecret
//
// IMPORTANT NOTES:
// - Off unless both are set: Development and staging keep the
//   address the browser asked for
// - The secret comes from an environment variable
//   (Hosting__EdgeSecret), never from a committed file
// - Cloudflare's addresses alone prove nothing: every
//   Cloudflare customer's Worker comes from them
// =========================================================
public sealed class VixlyEdgeSettings
{
    public const int SecretMinLength = 32;

    private VixlyEdgeSettings(IReadOnlyList<string> hosts, string secret)
    {
        Hosts = hosts;
        Secret = secret;
    }

    public IReadOnlyList<string> Hosts { get; }

    public string Secret { get; }

    public bool Enabled => Hosts.Count > 0 && Secret.Length >= SecretMinLength;

    public static VixlyEdgeSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string[] hosts = (configuration.GetSection("Hosting:EdgeHosts").Get<string[]>() ?? Array.Empty<string>())
            .Where(host => !string.IsNullOrWhiteSpace(host))
            .Select(host => host.Trim().ToLowerInvariant())
            .ToArray();

        return new VixlyEdgeSettings(hosts, configuration["Hosting:EdgeSecret"]?.Trim() ?? string.Empty);
    }
}
