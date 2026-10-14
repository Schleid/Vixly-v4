using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Infrastructure.Hosting;

// =========================================================
// FILE: PlatformPublicLinks.cs
//
// PURPOSE:
// Builds links to the public site from the hosting settings.
//
// RESPONSIBILITIES:
// - Build an offer's registration link (D-S21)
//
// IMPORTANT NOTES:
// - PlatformHosting:PublicBaseUrl; the token is URL-escaped
// =========================================================
public sealed class PlatformPublicLinks : IPlatformPublicLinks
{
    private readonly PlatformHostingOptions _hosting;

    public PlatformPublicLinks(IOptions<PlatformHostingOptions> hosting)
    {
        _hosting = hosting?.Value ?? throw new ArgumentNullException(nameof(hosting));
    }

    public string OfferLink(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return _hosting.PublicBaseUrl.TrimEnd('/') + "/register?offer=" + Uri.EscapeDataString(token);
    }
}
