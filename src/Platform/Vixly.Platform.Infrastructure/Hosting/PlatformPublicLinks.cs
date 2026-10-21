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

    private readonly Tenants.PlatformTenantOptions _tenants;

    public PlatformPublicLinks(IOptions<PlatformHostingOptions> hosting, IOptions<Tenants.PlatformTenantOptions> tenants)
    {
        _hosting = hosting?.Value ?? throw new ArgumentNullException(nameof(hosting));
        _tenants = tenants?.Value ?? throw new ArgumentNullException(nameof(tenants));
    }

    public string OfferLink(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return _hosting.PublicBaseUrl.TrimEnd('/') + "/register?offer=" + Uri.EscapeDataString(token);
    }

    public string PayLink(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return _hosting.PublicBaseUrl.TrimEnd('/') + "/pay/" + Uri.EscapeDataString(token);
    }

    public string SaveCardLink(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return _hosting.PublicBaseUrl.TrimEnd('/') + "/cards/" + Uri.EscapeDataString(token);
    }

    public string RegistrationPayment()
    {
        return _hosting.PublicBaseUrl.TrimEnd('/') + "/register/payment";
    }

    // Slice 7: served by Operations on the tenant host (H-1); the token after "#" never reaches a server log.
    public string DeviceLink(string portalSlug, Vixly.Platform.Domain.Licensing.PlatformDeviceLinkKind kind, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portalSlug);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        string page = kind == Vixly.Platform.Domain.Licensing.PlatformDeviceLinkKind.FirstDevice ? "devices/first" : "devices/recover";
        return Portal(portalSlug) + "/" + page + "#" + Uri.EscapeDataString(token);
    }

    // Slice 8: the owner reads and accepts the Terms in Operations on the tenant host.
    public string LegalLink(string portalSlug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portalSlug);
        return Portal(portalSlug) + "/legal";
    }

    public string PortalLink(string portalSlug, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portalSlug);
        ArgumentNullException.ThrowIfNull(path);
        return Portal(portalSlug) + "/" + path.TrimStart('/');
    }

    public string SecondFactorResetLink(string portalSlug, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portalSlug);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Portal(portalSlug) + "/account/second-factor-reset#" + Uri.EscapeDataString(token);
    }

    private string Portal(string portalSlug)
    {
        string template = _tenants.PortalUrlTemplate?.Trim() ?? string.Empty;

        return template.Length > 0
            ? template.Replace("{slug}", portalSlug, StringComparison.Ordinal).TrimEnd('/')
            : "https://" + portalSlug + "." + _tenants.TenantBaseDomain.Trim().TrimEnd('/');
    }
}
