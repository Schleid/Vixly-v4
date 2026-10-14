using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformTenantRegistrationRepository.cs
//
// PURPOSE:
// EF implementation of IPlatformTenantRegistrationRepository.
//
// RESPONSIBILITIES:
// - Find registrations by id or access hash
// - Find and list verification links
//
// IMPORTANT NOTES:
// - Lookups by hash use unique indexes
// =========================================================
internal sealed class PlatformTenantRegistrationRepository : IPlatformTenantRegistrationRepository
{
    private readonly PlatformDbContext _context;

    public PlatformTenantRegistrationRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformTenantRegistration?> GetAsync(Guid registrationId, CancellationToken cancellationToken)
    {
        return _context.TenantRegistrations.FirstOrDefaultAsync(item => item.Id == registrationId, cancellationToken);
    }

    public Task<PlatformTenantRegistration?> FindByAccessHashAsync(string accessTokenHash, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accessTokenHash);

        return _context.TenantRegistrations.FirstOrDefaultAsync(item => item.AccessTokenHash == accessTokenHash, cancellationToken);
    }

    public Task<PlatformRegistrationToken?> FindTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);

        return _context.RegistrationTokens.FirstOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformRegistrationToken>> ListUnusedTokensAsync(Guid registrationId, CancellationToken cancellationToken)
    {
        return await _context.RegistrationTokens
            .Where(item => item.PlatformTenantRegistrationId == registrationId && item.UsedUtc == null && item.RevokedUtc == null)
            .ToListAsync(cancellationToken);
    }

    public void Add(PlatformTenantRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        _context.TenantRegistrations.Add(registration);
    }

    public void AddToken(PlatformRegistrationToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        _context.RegistrationTokens.Add(token);
    }
}
