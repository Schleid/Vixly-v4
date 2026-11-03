using Vixly.Operations.Domain.Items;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsItemRepository.cs
//
// PURPOSE:
// Reads and changes the items of parcels and pre-alerts, and the company's item catalogue (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - A parcel's items: its own, or its pre-alert's until staff save them
// - The parcels at origin with an item to review
// - The catalogue
//
// IMPORTANT NOTES:
// - The unit of work saves
// =========================================================
public interface IOperationsItemRepository
{
    // The parcel's own items, else those of its pre-alert that no parcel took yet; in line order.
    Task<IReadOnlyList<OperationsParcelItem>> ListForParcelAsync(Guid parcelId, Guid? preAlertId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsParcelItem>> ListForParcelsAsync(IReadOnlyCollection<OperationsParcel> parcels, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsParcelItem>> ListForPreAlertAsync(Guid preAlertId, CancellationToken cancellationToken);

    void Add(OperationsParcelItem item);

    void Remove(OperationsParcelItem item);

    // Parcels at origin, not yet in a bundle or a box, with an item lacking its customs code or origin (or, from the given moment, with no item at all); oldest first.
    Task<IReadOnlyList<OperationsParcel>> ListParcelsToReviewAsync(IReadOnlyCollection<Guid> warehouseIds, DateTimeOffset? itemsRequiredSinceUtc, int take,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsItemCatalogueEntry>> ListCatalogueAsync(CancellationToken cancellationToken);

    Task<OperationsItemCatalogueEntry?> GetCatalogueEntryAsync(Guid entryId, CancellationToken cancellationToken);

    void AddCatalogueEntry(OperationsItemCatalogueEntry entry);

    void RemoveCatalogueEntry(OperationsItemCatalogueEntry entry);
}
