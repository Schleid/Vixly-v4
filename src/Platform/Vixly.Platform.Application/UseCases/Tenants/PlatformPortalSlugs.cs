using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants;

// =========================================================
// FILE: PlatformPortalSlugs.cs
//
// PURPOSE:
// Checks and claims portal slugs (D-T10, D-T11).
//
// RESPONSIBILITIES:
// - Normalise a requested slug and refuse reserved names
// - Tell whether a slug is free, and hand back a lapsed claim
//   that may be taken over
//
// IMPORTANT NOTES:
// - Reserved names: the H-2 floor plus configuration
// - The database unique index has the last word on races
// =========================================================
public sealed class PlatformPortalSlugs
{
    private readonly IPlatformPortalSlugRepository _slugs;

    private readonly IPlatformTenantPolicy _policy;

    public PlatformPortalSlugs(IPlatformPortalSlugRepository slugs, IPlatformTenantPolicy policy)
    {
        _slugs = slugs ?? throw new ArgumentNullException(nameof(slugs));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public async Task<PlatformSlugCheck> CheckAsync(string? requested, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        if (!PlatformPortalSlugFormat.TryNormalize(requested, out string? slug))
        {
            return PlatformSlugCheck.Refused(PlatformTenantErrors.SlugInvalid);
        }

        if (PlatformPortalSlugFormat.IsReserved(slug, _policy.ReservedSlugs))
        {
            return PlatformSlugCheck.Refused(PlatformTenantErrors.SlugReserved);
        }

        PlatformPortalSlug? claim = await _slugs.FindAsync(slug, cancellationToken);

        if (claim is not null && !claim.IsLapsed(utcNow))
        {
            return PlatformSlugCheck.Refused(PlatformTenantErrors.SlugTaken);
        }

        return PlatformSlugCheck.Free(slug, claim);
    }
}
