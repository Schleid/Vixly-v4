using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants;

// =========================================================
// FILE: PlatformSlugCheck.cs
//
// PURPOSE:
// The outcome of checking a portal slug.
//
// RESPONSIBILITIES:
// - Carry the normalised slug and any lapsed claim, or the reason
//   it cannot be used
//
// IMPORTANT NOTES:
// - Built only by PlatformPortalSlugs
// =========================================================
public sealed class PlatformSlugCheck
{
    private PlatformSlugCheck(string? slug, PlatformPortalSlug? lapsedClaim, Error? error)
    {
        Slug = slug;
        LapsedClaim = lapsedClaim;
        Error = error;
    }

    public string? Slug { get; }

    public PlatformPortalSlug? LapsedClaim { get; }

    public Error? Error { get; }

    public static PlatformSlugCheck Free(string slug, PlatformPortalSlug? lapsedClaim)
    {
        return new PlatformSlugCheck(slug, lapsedClaim, null);
    }

    public static PlatformSlugCheck Refused(Error error)
    {
        return new PlatformSlugCheck(null, null, error);
    }
}
