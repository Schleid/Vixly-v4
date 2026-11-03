using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsSiteRepository.cs
//
// PURPOSE:
// The brand, texts and images of the current tenant site (Slice 1B).
//
// RESPONSIBILITIES:
// - Load the brand with its texts
// - Add and load images
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1)
// =========================================================
public interface IOperationsSiteRepository
{
    Task<OperationsTenantBrand?> FindBrandAsync(CancellationToken cancellationToken);

    void AddImage(OperationsSiteImage image);

    Task<OperationsSiteImage?> GetImageAsync(Guid imageId, CancellationToken cancellationToken);

    // Customer testimonials (owner, 2026-10-30), in the company's order.
    Task<IReadOnlyList<OperationsSiteTestimonial>> ListTestimonialsAsync(CancellationToken cancellationToken);

    Task<OperationsSiteTestimonial?> GetTestimonialAsync(Guid testimonialId, CancellationToken cancellationToken);

    void AddTestimonial(OperationsSiteTestimonial testimonial);

    void RemoveTestimonial(OperationsSiteTestimonial testimonial);

    // Packages of shipments picked up or delivered, from the company's records (owner, 2026-10-30).
    Task<int> CountDeliveredPackagesAsync(CancellationToken cancellationToken);
}
