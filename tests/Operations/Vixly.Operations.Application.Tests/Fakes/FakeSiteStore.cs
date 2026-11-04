using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakeSiteStore.cs
//
// PURPOSE:
// In-memory site, image and location storage for the Slice 1B use cases.
//
// RESPONSIBILITIES:
// - Keep the brand, images, branches, warehouses and contacts
// - Inspect images by a fixed answer
//
// IMPORTANT NOTES:
// - Tests only; changes are kept at once (the identity store is the unit of work)
// =========================================================
internal sealed class FakeSiteStore : IOperationsSiteRepository, IOperationsLocationRepository, IOperationsImageInspector
{
    public OperationsTenantBrand? Brand { get; set; }

    public List<OperationsSiteImage> Images { get; } = new();

    public List<OperationsBranch> Branches { get; } = new();

    public List<OperationsWarehouse> Warehouses { get; } = new();

    public List<OperationsCountryContact> Contacts { get; } = new();

    // What the inspector reports for any content; null means "not an image we accept".
    public OperationsImageInfo? Inspection { get; set; }

    public Task<OperationsTenantBrand?> FindBrandAsync(CancellationToken cancellationToken) => Task.FromResult(Brand);

    public void AddImage(OperationsSiteImage image) => Images.Add(image);

    public Task<OperationsSiteImage?> GetImageAsync(Guid imageId, CancellationToken cancellationToken) => Task.FromResult(Images.FirstOrDefault(image => image.Id == imageId));

    public List<OperationsSiteTestimonial> Testimonials { get; } = new();

    public int DeliveredPackages { get; set; }

    public Task<IReadOnlyList<OperationsSiteTestimonial>> ListTestimonialsAsync(Vixly.Operations.Application.Site.OperationsTestimonialView view, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsSiteTestimonial>>(Testimonials.Where(item => view switch
        {
            Vixly.Operations.Application.Site.OperationsTestimonialView.New => item.IsNew,
            Vixly.Operations.Application.Site.OperationsTestimonialView.Published => item.Shown,
            Vixly.Operations.Application.Site.OperationsTestimonialView.Hidden => !item.Shown && !item.IsNew,
            Vixly.Operations.Application.Site.OperationsTestimonialView.Staff => !item.FromCustomer,
            _ => true
        }).OrderBy(item => item.SortOrder).ThenBy(item => item.CreatedUtc).Take(take).ToList());

    public Task<OperationsSiteTestimonial?> FindCustomerReviewAsync(Guid customerId, CancellationToken cancellationToken) =>
        Task.FromResult(Testimonials.FirstOrDefault(item => item.CustomerId == customerId));

    public Task<int> CountNewReviewsAsync(CancellationToken cancellationToken) => Task.FromResult(Testimonials.Count(item => item.IsNew));

    // Customer reviews: the customers whose last finished shipment the test sets.
    public Dictionary<Guid, Vixly.Operations.Application.Site.OperationsReviewShipment> FinishedShipments { get; } = new();

    public Task<Vixly.Operations.Application.Site.OperationsReviewShipment?> FindLastFinishedShipmentAsync(Guid customerId, CancellationToken cancellationToken) =>
        Task.FromResult(FinishedShipments.TryGetValue(customerId, out Vixly.Operations.Application.Site.OperationsReviewShipment? shipment) ? shipment : null);

    public Task<OperationsSiteTestimonial?> GetTestimonialAsync(Guid testimonialId, CancellationToken cancellationToken) =>
        Task.FromResult(Testimonials.FirstOrDefault(item => item.Id == testimonialId));

    public void AddTestimonial(OperationsSiteTestimonial testimonial) => Testimonials.Add(testimonial);

    public void RemoveTestimonial(OperationsSiteTestimonial testimonial) => Testimonials.Remove(testimonial);

    public Task<int> CountDeliveredPackagesAsync(CancellationToken cancellationToken) => Task.FromResult(DeliveredPackages);

    public void Add(OperationsBranch branch) => Branches.Add(branch);

    public void Add(OperationsWarehouse warehouse) => Warehouses.Add(warehouse);

    public void Add(OperationsCountryContact contact) => Contacts.Add(contact);

    public void Remove(OperationsCountryContact contact) => Contacts.Remove(contact);

    public Task<OperationsBranch?> GetBranchAsync(Guid branchId, CancellationToken cancellationToken) => Task.FromResult(Branches.FirstOrDefault(branch => branch.Id == branchId));

    public Task<OperationsWarehouse?> GetWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken) => Task.FromResult(Warehouses.FirstOrDefault(warehouse => warehouse.Id == warehouseId));

    public Task<OperationsCountryContact?> GetContactAsync(Guid contactId, CancellationToken cancellationToken) => Task.FromResult(Contacts.FirstOrDefault(contact => contact.Id == contactId));

    public Task<IReadOnlyList<OperationsBranch>> ListBranchesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OperationsBranch>>(Branches.ToList());

    public Task<IReadOnlyList<OperationsWarehouse>> ListWarehousesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OperationsWarehouse>>(Warehouses.ToList());

    public Task<IReadOnlyList<OperationsCountryContact>> ListContactsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OperationsCountryContact>>(Contacts.ToList());

    public Task<bool> CodeTakenAsync(string code, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Branches.Any(branch => branch.Code == code && branch.Id != exceptId) || Warehouses.Any(warehouse => warehouse.Code == code && warehouse.Id != exceptId));

    public OperationsImageInfo? Inspect(byte[] content) => Inspection;
}
