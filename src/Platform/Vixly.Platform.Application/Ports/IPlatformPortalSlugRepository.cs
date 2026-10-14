using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformPortalSlugRepository.cs
//
// PURPOSE:
// Loads and stores portal slug claims (D-T10).
//
// RESPONSIBILITIES:
// - Find a claim by slug or by its tenant
// - Add and remove claims
//
// IMPORTANT NOTES:
// - The table has a unique index on the slug: a racing insert
//   fails the save, which the use case reports as slug_taken
// =========================================================
public interface IPlatformPortalSlugRepository
{
    Task<PlatformPortalSlug?> FindAsync(string slug, CancellationToken cancellationToken);

    Task<PlatformPortalSlug?> FindPermanentForTenantAsync(Guid platformTenantId, CancellationToken cancellationToken);

    void Add(PlatformPortalSlug claim);

    void Remove(PlatformPortalSlug claim);
}
