using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformPortalSlugRepository.cs
//
// PURPOSE:
// EF implementation of IPlatformPortalSlugRepository.
//
// RESPONSIBILITIES:
// - Find, add and remove slug claims
//
// IMPORTANT NOTES:
// - The unique index on Slug settles races at save time
// =========================================================
internal sealed class PlatformPortalSlugRepository : IPlatformPortalSlugRepository
{
    private readonly PlatformDbContext _context;

    public PlatformPortalSlugRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformPortalSlug?> FindAsync(string slug, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(slug);

        return _context.PortalSlugs.FirstOrDefaultAsync(claim => claim.Slug == slug, cancellationToken);
    }

    public Task<PlatformPortalSlug?> FindPermanentForTenantAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return _context.PortalSlugs.FirstOrDefaultAsync(
            claim => claim.PlatformTenantId == platformTenantId && claim.State == PlatformPortalSlugState.Permanent,
            cancellationToken);
    }

    public void Add(PlatformPortalSlug claim)
    {
        ArgumentNullException.ThrowIfNull(claim);

        _context.PortalSlugs.Add(claim);
    }

    public void Remove(PlatformPortalSlug claim)
    {
        ArgumentNullException.ThrowIfNull(claim);

        _context.PortalSlugs.Remove(claim);
    }
}
