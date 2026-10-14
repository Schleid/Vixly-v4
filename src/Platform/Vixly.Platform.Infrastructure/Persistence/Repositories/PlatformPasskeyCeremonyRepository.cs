using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformPasskeyCeremonyRepository.cs
//
// PURPOSE:
// EF implementation of IPlatformPasskeyCeremonyRepository.
//
// RESPONSIBILITIES:
// - Find by token hash; add
//
// IMPORTANT NOTES:
// - Uses the unique index on TokenHash
// =========================================================
internal sealed class PlatformPasskeyCeremonyRepository : IPlatformPasskeyCeremonyRepository
{
    private readonly PlatformDbContext _context;

    public PlatformPasskeyCeremonyRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformPasskeyCeremony?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);

        return _context.PasskeyCeremonies.FirstOrDefaultAsync(ceremony => ceremony.TokenHash == tokenHash, cancellationToken);
    }

    public void Add(PlatformPasskeyCeremony ceremony)
    {
        ArgumentNullException.ThrowIfNull(ceremony);

        _context.PasskeyCeremonies.Add(ceremony);
    }
}
