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
// =========================================================
internal sealed class OperationsBrandRepository : IOperationsBrandRepository
{
    private readonly OperationsDbContext _context;

    public OperationsBrandRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<OperationsTenantBrand?> FindAsync(CancellationToken cancellationToken)
    {
        return _context.TenantBrands.FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(OperationsTenantBrand brand)
    {
        ArgumentNullException.ThrowIfNull(brand);
        _context.TenantBrands.Add(brand);
    }
}
