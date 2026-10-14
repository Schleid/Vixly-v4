using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformSalesSettingsRepository.cs
//
// PURPOSE:
// Loads and stores the one Sales settings row (D-S22).
//
// RESPONSIBILITIES:
// - Implement IPlatformSalesSettingsRepository with EF Core
//
// IMPORTANT NOTES:
// - Changes are saved by the unit of work
// =========================================================
internal sealed class PlatformSalesSettingsRepository : IPlatformSalesSettingsRepository
{
    private readonly PlatformDbContext _context;

    public PlatformSalesSettingsRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformSalesSettings?> GetAsync(CancellationToken cancellationToken)
    {
        return _context.SalesSettings.FirstOrDefaultAsync(settings => settings.Key == PlatformSalesSettings.SingletonKey, cancellationToken);
    }

    public void Add(PlatformSalesSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _context.SalesSettings.Add(settings);
    }
}
