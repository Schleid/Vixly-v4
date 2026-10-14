using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Infrastructure.Tenants;

// =========================================================
// FILE: ConfiguredPlatformTenantPolicy.cs
//
// PURPOSE:
// IPlatformTenantPolicy read from configuration.
//
// RESPONSIBILITIES:
// - Expose PlatformTenantOptions to the Application
//
// IMPORTANT NOTES:
// - Values are read once per scope
// =========================================================
internal sealed class ConfiguredPlatformTenantPolicy : IPlatformTenantPolicy
{
    private readonly PlatformTenantOptions _options;

    public ConfiguredPlatformTenantPolicy(IOptions<PlatformTenantOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public IReadOnlyCollection<string> ReservedSlugs => _options.ReservedSlugs;

    public string TermsVersion => _options.TermsVersion;

    public string PrivacyVersion => _options.PrivacyVersion;

    public string TenantBaseDomain => _options.TenantBaseDomain;
}
