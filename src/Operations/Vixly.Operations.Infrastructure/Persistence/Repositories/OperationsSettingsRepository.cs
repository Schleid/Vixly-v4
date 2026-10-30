using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsSettingsRepository.cs
//
// PURPOSE:
// Reads and adds the tenant's settings row (Slice 1E).
//
// RESPONSIBILITIES:
// - Find the current tenant's row through the tenant filter
//
// IMPORTANT NOTES:
// - No saving here: the unit of work saves
// =========================================================
internal sealed class OperationsSettingsRepository : IOperationsSettingsRepository
{
    private readonly OperationsDbContext _context;

    public OperationsSettingsRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsTenantSettings settings) => _context.TenantSettings.Add(settings ?? throw new ArgumentNullException(nameof(settings)));

    public Task<OperationsTenantSettings?> FindAsync(CancellationToken cancellationToken) => _context.TenantSettings.FirstOrDefaultAsync(cancellationToken);
}
