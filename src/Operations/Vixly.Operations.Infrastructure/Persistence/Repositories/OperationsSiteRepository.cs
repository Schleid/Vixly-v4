using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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

    private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

    public OperationsSiteRepository(OperationsDbContext context, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
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

    public async Task<IReadOnlyList<OperationsSiteTestimonial>> ListTestimonialsAsync(CancellationToken cancellationToken) =>
        await _context.SiteTestimonials.OrderBy(item => item.SortOrder).ThenBy(item => item.CreatedUtc).ToListAsync(cancellationToken);

    public Task<OperationsSiteTestimonial?> GetTestimonialAsync(Guid testimonialId, CancellationToken cancellationToken) =>
        _context.SiteTestimonials.FirstOrDefaultAsync(item => item.Id == testimonialId, cancellationToken);

    public void AddTestimonial(OperationsSiteTestimonial testimonial) => _context.SiteTestimonials.Add(testimonial ?? throw new ArgumentNullException(nameof(testimonial)));

    public void RemoveTestimonial(OperationsSiteTestimonial testimonial) => _context.SiteTestimonials.Remove(testimonial ?? throw new ArgumentNullException(nameof(testimonial)));

    // The home page asks on every visit: counted once an hour per company.
    public async Task<int> CountDeliveredPackagesAsync(CancellationToken cancellationToken)
    {
        string key = "ops:site:delivered:" + _context.TenantFilterId.ToString("N");

        if (_cache.TryGetValue(key, out int cached))
        {
            return cached;
        }

        int count = await (from parcel in _context.Parcels
                           join shipment in _context.Shipments on parcel.ShipmentId equals shipment.Id
                           where shipment.PickedUpUtc != null || shipment.DeliveredUtc != null
                           select parcel.Id).CountAsync(cancellationToken);
        _cache.Set(key, count, TimeSpan.FromHours(1));
        return count;
    }
}
