using Vixly.Operations.Domain.Arrivals;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsArrivalRepository.cs
//
// PURPOSE:
// Reads and writes what destination receiving needs (Slice 7 step 1).
//
// RESPONSIBILITIES:
// - The dispatches bound for a country, the discrepancies, the parcels scanned
//
// IMPORTANT NOTES:
// - Tenant-scoped by the query filter
// =========================================================
public interface IOperationsArrivalRepository
{
    // Departed or arrived and received at this warehouse (or, for a country only, not yet claimed in its country); not received yet, or received lately.
    Task<IReadOnlyList<OperationsDispatch>> ListArrivingAsync(Guid warehouseId, string countryCode, bool received, int take, CancellationToken cancellationToken);

    void Add(OperationsArrivalDiscrepancy discrepancy);

    Task<OperationsArrivalDiscrepancy?> GetDiscrepancyAsync(Guid discrepancyId, CancellationToken cancellationToken);

    // The open ones about these bundles or parcels, on any dispatch; tracked.
    Task<IReadOnlyList<OperationsArrivalDiscrepancy>> ListOpenForAsync(IReadOnlyCollection<Guid> bundleIds, IReadOnlyCollection<Guid> parcelIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsArrivalDiscrepancy>> ListDispatchDiscrepanciesAsync(Guid dispatchId, CancellationToken cancellationToken);

    // At these warehouses, or on a dispatch that left one of them (the origin sees them too, R-2).
    Task<IReadOnlyList<(OperationsArrivalDiscrepancy Item, string? DispatchNumber, Guid? OriginWarehouseId)>> ListDiscrepanciesAsync(IReadOnlyCollection<Guid> warehouseIds, bool openOnly, int take,
        CancellationToken cancellationToken);

    Task<OperationsParcel?> FindParcelForUpdateAsync(string number, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsParcel>> ListBoxParcelsForUpdateAsync(Guid boxId, CancellationToken cancellationToken);
}
