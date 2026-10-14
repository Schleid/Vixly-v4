using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformSignInChallengeRepository.cs
//
// PURPOSE:
// EF implementation of IPlatformSignInChallengeRepository.
//
// RESPONSIBILITIES:
// - Find by token hash; add
//
// IMPORTANT NOTES:
// - Uses the unique index on TokenHash
// =========================================================
internal sealed class PlatformSignInChallengeRepository : IPlatformSignInChallengeRepository
{
    private readonly PlatformDbContext _context;

    public PlatformSignInChallengeRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformSignInChallenge?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);

        return _context.SignInChallenges.FirstOrDefaultAsync(challenge => challenge.TokenHash == tokenHash, cancellationToken);
    }

    public void Add(PlatformSignInChallenge challenge)
    {
        ArgumentNullException.ThrowIfNull(challenge);

        _context.SignInChallenges.Add(challenge);
    }
}
