using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Items;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsItemRepository.cs
//
// PURPOSE:
// Reads and changes parcel items and the item catalogue (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Implement IOperationsItemRepository on the Operations database
//
// IMPORTANT NOTES:
// - The unit of work saves
// - The tenant filter applies to every query
// =========================================================
internal sealed class OperationsItemRepository : IOperationsItemRepository
{
    private readonly OperationsDbContext _context;

    public OperationsItemRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<OperationsParcelItem>> ListForParcelAsync(Guid parcelId, Guid? preAlertId, CancellationToken cancellationToken)
    {
        List<OperationsParcelItem> own = await _context.ParcelItems.Where(item => item.ParcelId == parcelId).OrderBy(item => item.Line).ToListAsync(cancellationToken);

        return own.Count > 0 || preAlertId is not { } preAlert
            ? own
            : await _context.ParcelItems.Where(item => item.ParcelId == null && item.PreAlertId == preAlert).OrderBy(item => item.Line).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsParcelItem>> ListForParcelsAsync(IReadOnlyCollection<OperationsParcel> parcels, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcels);
        List<Guid> parcelIds = parcels.Select(parcel => parcel.Id).ToList();
        List<Guid> preAlertIds = parcels.Where(parcel => parcel.PreAlertId is not null).Select(parcel => parcel.PreAlertId!.Value).ToList();

        if (parcelIds.Count == 0)
        {
            return Array.Empty<OperationsParcelItem>();
        }

        return await _context.ParcelItems
            .Where(item => (item.ParcelId != null && parcelIds.Contains(item.ParcelId.Value)) || (item.ParcelId == null && item.PreAlertId != null && preAlertIds.Contains(item.PreAlertId.Value)))
            .OrderBy(item => item.Line)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsParcelItem>> ListForPreAlertAsync(Guid preAlertId, CancellationToken cancellationToken) =>
        await _context.ParcelItems.Where(item => item.PreAlertId == preAlertId).OrderBy(item => item.Line).ToListAsync(cancellationToken);

    public void Add(OperationsParcelItem item) => _context.ParcelItems.Add(item ?? throw new ArgumentNullException(nameof(item)));

    public void Remove(OperationsParcelItem item) => _context.ParcelItems.Remove(item ?? throw new ArgumentNullException(nameof(item)));

    public async Task<IReadOnlyList<OperationsParcel>> ListParcelsToReviewAsync(IReadOnlyCollection<Guid> warehouseIds, DateTimeOffset? itemsRequiredSinceUtc, int take,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);
        List<Guid> warehouses = warehouseIds.ToList();
        IQueryable<OperationsParcelItem> items = _context.ParcelItems;

        IQueryable<OperationsParcel> atOrigin = _context.Parcels.Where(parcel => parcel.WarehouseId != null && warehouses.Contains(parcel.WarehouseId.Value)
            && parcel.CustomerId != null && parcel.BundleId == null && parcel.BoxId == null
            && parcel.Milestone >= OperationsMilestone.ReceivedAtOrigin && parcel.Milestone < OperationsMilestone.Consolidated);

        IQueryable<OperationsParcel> toReview = atOrigin.Where(parcel =>
            items.Any(item => (item.ParcelId == parcel.Id || (item.ParcelId == null && parcel.PreAlertId != null && item.PreAlertId == parcel.PreAlertId))
                && (item.HsCode == null || item.OriginCountryCode == null))
            || (itemsRequiredSinceUtc != null && parcel.CreatedUtc >= itemsRequiredSinceUtc
                && !items.Any(item => item.ParcelId == parcel.Id || (item.ParcelId == null && parcel.PreAlertId != null && item.PreAlertId == parcel.PreAlertId))));

        return await toReview.OrderBy(parcel => parcel.CreatedUtc).Take(take).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsItemCatalogueEntry>> ListCatalogueAsync(CancellationToken cancellationToken) =>
        await _context.ItemCatalogue.OrderBy(entry => entry.Description).ToListAsync(cancellationToken);

    public Task<OperationsItemCatalogueEntry?> GetCatalogueEntryAsync(Guid entryId, CancellationToken cancellationToken) =>
        _context.ItemCatalogue.FirstOrDefaultAsync(entry => entry.Id == entryId, cancellationToken);

    public void AddCatalogueEntry(OperationsItemCatalogueEntry entry) => _context.ItemCatalogue.Add(entry ?? throw new ArgumentNullException(nameof(entry)));

    public void RemoveCatalogueEntry(OperationsItemCatalogueEntry entry) => _context.ItemCatalogue.Remove(entry ?? throw new ArgumentNullException(nameof(entry)));
}
