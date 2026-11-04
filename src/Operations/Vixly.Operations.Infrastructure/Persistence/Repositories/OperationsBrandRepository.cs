using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsBrandRepository.cs
//
// PURPOSE:
// The brand of the current tenant.
//
// RESPONSIBILITIES:
// - Implement IOperationsBrandRepository with EF Core
//
// IMPORTANT NOTES:
// - Tenant-filtered by the query filter (D-F1)
// - Harmonization H-1 (owner, 2026-11-04): the name and the published settings are read on their own, one small command,
//   never with every website text; a brand already loaded in this request answers instead, so an unsaved change shows as before
// =========================================================
internal sealed class OperationsBrandRepository : IOperationsBrandRepository
{
    private readonly OperationsDbContext _context;

    public OperationsBrandRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // P-3: read once per request (forgotten at the next save).
    public Task<OperationsTenantBrand?> FindAsync(CancellationToken cancellationToken)
    {
        return _context.RememberAsync("brand", () => _context.TenantBrands.FirstOrDefaultAsync(cancellationToken));
    }

    public async Task<string?> FindDisplayNameAsync(CancellationToken cancellationToken)
    {
        return Loaded() is { } loaded
            ? loaded.DisplayName
            : await _context.RememberAsync("brand-name", () => _context.TenantBrands.AsNoTracking().Select(brand => brand.Published.DisplayName).FirstOrDefaultAsync(cancellationToken));
    }

    public async Task<OperationsBrandSettings?> FindPublishedAsync(CancellationToken cancellationToken)
    {
        return Loaded() is { } loaded
            ? loaded.Published
            : await _context.RememberAsync("brand-published", () => _context.TenantBrands.AsNoTracking().Select(brand => brand.Published).FirstOrDefaultAsync(cancellationToken));
    }

    public void Add(OperationsTenantBrand brand)
    {
        ArgumentNullException.ThrowIfNull(brand);
        _context.TenantBrands.Add(brand);
    }

    public async Task<IReadOnlyList<Guid>> ListTenantIdsAsync(CancellationToken cancellationToken)
    {
        return await _context.TenantBrands.AsNoTracking().Select(brand => brand.TenantId).Distinct().ToListAsync(cancellationToken);
    }

    // The tenant's brand if this request already loaded it (one per tenant; the context serves one tenant).
    // Looked up without a change scan: only whether it is loaded matters, not what changed.
    private OperationsTenantBrand? Loaded()
    {
        bool detect = _context.ChangeTracker.AutoDetectChangesEnabled;
        _context.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            // Only the current company's brand (review, 2026-11-04): a context never serves two companies, and this holds even if it did.
            return _context.ChangeTracker.Entries<OperationsTenantBrand>()
                .FirstOrDefault(entry => entry.State != EntityState.Deleted && (_context.IgnoreTenantFilter || entry.Entity.TenantId == _context.TenantFilterId))?.Entity;
        }
        finally
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = detect;
        }
    }
}
