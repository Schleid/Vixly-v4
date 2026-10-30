using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsInspectionRepository.cs
//
// PURPOSE:
// Stores the restricted-items list, the inspection rules and the inspections; finds held parcels (Slice 3C).
//
// RESPONSIBILITIES:
// - Add and read list entries, rules and inspections
// - Page the inspection queue and the held parcels of chosen warehouses, oldest first
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1)
// - Entries and rules are read whole: a company keeps at most a few hundred
// =========================================================
public interface IOperationsInspectionRepository
{
    void Add(OperationsRestrictedItem item);

    void Add(OperationsInspectionRule rule);

    void Add(OperationsInspection inspection);

    Task<IReadOnlyList<OperationsRestrictedItem>> ListItemsAsync(CancellationToken cancellationToken);

    Task<OperationsRestrictedItem?> GetItemAsync(Guid itemId, CancellationToken cancellationToken);

    Task<OperationsRestrictedItem?> FindItemByNameAsync(string name, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsInspectionRule>> ListRulesAsync(CancellationToken cancellationToken);

    Task<OperationsInspectionRule?> GetRuleAsync(Guid ruleId, CancellationToken cancellationToken);

    Task<OperationsInspection?> GetInspectionAsync(Guid inspectionId, CancellationToken cancellationToken);

    // The open inspection of a parcel, or its latest one.
    Task<OperationsInspection?> FindLatestAsync(Guid parcelId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsInspection>> ListLatestAsync(IReadOnlyCollection<Guid> parcelIds, CancellationToken cancellationToken);

    // Waiting and in progress, oldest first.
    Task<(IReadOnlyList<OperationsInspection> Items, int TotalCount)> ListQueueAsync(IReadOnlyCollection<Guid> warehouseIds, int page, int pageSize, CancellationToken cancellationToken,
        Guid? parcelId = null);

    Task<IReadOnlyList<OperationsParcel>> ListParcelsAsync(IReadOnlyCollection<Guid> parcelIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsInspection>> ListAwaitingApprovalAsync(IReadOnlyCollection<Guid> warehouseIds, CancellationToken cancellationToken);

    // Parcels with an open hold, oldest first; reason None means any.
    Task<(IReadOnlyList<OperationsParcel> Items, int TotalCount)> ListHeldAsync(IReadOnlyCollection<Guid> warehouseIds, OperationsHoldReasons reason, int page, int pageSize,
        CancellationToken cancellationToken);
}
