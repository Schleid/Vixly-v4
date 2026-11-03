using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformSecuritySettingsRepository.cs
//
// PURPOSE:
// Stores Control's sign-in rule in platform.SecuritySettings.
//
// RESPONSIBILITIES:
// - Read and add the one row
//
// IMPORTANT NOTES:
// - No row means Require MFA = Yes
// =========================================================
internal sealed class PlatformSecuritySettingsRepository : IPlatformSecuritySettingsRepository
{
    private readonly PlatformDbContext _context;

    public PlatformSecuritySettingsRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformSecuritySettings?> GetAsync(CancellationToken cancellationToken)
    {
        return _context.SecuritySettings.FirstOrDefaultAsync(settings => settings.Key == PlatformSecuritySettings.SingletonKey, cancellationToken);
    }

    public void Add(PlatformSecuritySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _context.SecuritySettings.Add(settings);
    }
}
