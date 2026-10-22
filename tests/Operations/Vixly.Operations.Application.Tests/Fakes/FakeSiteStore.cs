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
