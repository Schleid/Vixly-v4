using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsSiteRepository.cs
//
// PURPOSE:
// The brand, texts and images of the current tenant site.
//
// RESPONSIBILITIES:
// - Implement IOperationsSiteRepository with EF Core
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1)
// =========================================================
internal sealed class OperationsSiteRepository : IOperationsSiteRepository
{
    private readonly OperationsDbContext _context;

    public OperationsSiteRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<OperationsTenantBrand?> FindBrandAsync(CancellationToken cancellationToken)
    {
        return _context.TenantBrands.FirstOrDefaultAsync(cancellationToken);
    }

    public void AddImage(OperationsSiteImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        _context.SiteImages.Add(image);
    }

    public Task<OperationsSiteImage?> GetImageAsync(Guid imageId, CancellationToken cancellationToken)
    {
        return _context.SiteImages.AsNoTracking().FirstOrDefaultAsync(image => image.Id == imageId, cancellationToken);
    }
}
