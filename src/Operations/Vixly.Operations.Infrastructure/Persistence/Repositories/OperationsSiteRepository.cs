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

    // Customer reviews (owner, 2026-11-03): one view, in the company's order, oldest first within a place.
    public async Task<IReadOnlyList<OperationsSiteTestimonial>> ListTestimonialsAsync(Vixly.Operations.Application.Site.OperationsTestimonialView view, int take, CancellationToken cancellationToken)
    {
        IQueryable<OperationsSiteTestimonial> items = _context.SiteTestimonials;
        items = view switch
        {
            Vixly.Operations.Application.Site.OperationsTestimonialView.New => items.Where(item => item.CustomerId != null && item.ReviewedUtc == null),
            Vixly.Operations.Application.Site.OperationsTestimonialView.Published => items.Where(item => item.Shown),
            Vixly.Operations.Application.Site.OperationsTestimonialView.Hidden => items.Where(item => !item.Shown && (item.CustomerId == null || item.ReviewedUtc != null)),
            Vixly.Operations.Application.Site.OperationsTestimonialView.Staff => items.Where(item => item.CustomerId == null),
            _ => items
        };

        return await items.OrderBy(item => item.SortOrder).ThenBy(item => item.CreatedUtc).ThenBy(item => item.Id).Take(Math.Clamp(take, 1, 1000)).ToListAsync(cancellationToken);
    }

    public Task<OperationsSiteTestimonial?> FindCustomerReviewAsync(Guid customerId, CancellationToken cancellationToken) =>
        _context.SiteTestimonials.FirstOrDefaultAsync(item => item.CustomerId == customerId, cancellationToken);

    public Task<int> CountNewReviewsAsync(CancellationToken cancellationToken) =>
        _context.SiteTestimonials.CountAsync(item => item.CustomerId != null && item.ReviewedUtc == null, cancellationToken);

    public async Task<Vixly.Operations.Application.Site.OperationsReviewShipment?> FindLastFinishedShipmentAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var last = await _context.Shipments.AsNoTracking()
            .Where(shipment => shipment.CustomerId == customerId && (shipment.PickedUpUtc != null || shipment.DeliveredUtc != null))
            .OrderByDescending(shipment => shipment.DeliveredUtc ?? shipment.PickedUpUtc)
            .Select(shipment => new { shipment.Id, Warehouse = shipment.DestinationWarehouseId ?? shipment.OriginWarehouseId, Reference = shipment.TrackingNumber ?? shipment.Number })
            .FirstOrDefaultAsync(cancellationToken);
        return last is null ? null : new Vixly.Operations.Application.Site.OperationsReviewShipment(last.Id, last.Warehouse, last.Reference ?? last.Id.ToString("N")[..12]);
    }

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
