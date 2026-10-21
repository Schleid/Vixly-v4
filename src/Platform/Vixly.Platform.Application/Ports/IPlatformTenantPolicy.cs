namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformTenantPolicy.cs
//
// PURPOSE:
// Configured values tenant management depends on.
//
// RESPONSIBILITIES:
// - Expose the configured reserved slugs (added to the H-2 floor)
// - Expose the current terms and privacy versions
// - Expose the tenant base domain ({slug}.<base domain>, H-1)
// - Say whether sign-up needs an invitation (an offer link)
//
// IMPORTANT NOTES:
// - Read from configuration; no secrets
// - Legal versions move to legal documents in Slice 8
// =========================================================
public interface IPlatformTenantPolicy
{
    IReadOnlyCollection<string> ReservedSlugs { get; }

    string TermsVersion { get; }

    string PrivacyVersion { get; }

    string TenantBaseDomain { get; }

    bool RegistrationInviteOnly { get; }
}
