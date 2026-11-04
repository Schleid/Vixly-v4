using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsSignInChallengeRepository.cs
//
// PURPOSE:
// Pending sign-ins of the current tenant.
//
// RESPONSIBILITIES:
// - Implement IOperationsSignInChallengeRepository with EF Core
//
// IMPORTANT NOTES:
// - Tenant-filtered by the query filter (D-F1)
// =========================================================
internal sealed class OperationsSignInChallengeRepository : IOperationsSignInChallengeRepository
{
    private readonly OperationsDbContext _context;

    public OperationsSignInChallengeRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsSignInChallenge challenge)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        _context.SignInChallenges.Add(challenge);
    }

    public Task<OperationsSignInChallenge?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _context.SignInChallenges.FirstOrDefaultAsync(challenge => challenge.TokenHash == tokenHash, cancellationToken);
    }
}
