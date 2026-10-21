using Vixly.Operations.Domain.Events;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsEventRepository.cs
//
// PURPOSE:
// Reads and writes what operational events need (Slice 6C).
//
// RESPONSIBILITIES:
// - The company's own causes
// - Find a subject by the number a person types
// - The copies of an event on parcels; a shipment's parcels for update
//
// IMPORTANT NOTES:
// - Tenant-scoped by the context's query filter
// =========================================================
public interface IOperationsEventRepository
{
    void Add(OperationsEventReason reason);

    Task<IReadOnlyList<OperationsEventReason>> ListReasonsAsync(CancellationToken cancellationToken);

    Task<OperationsEventReason?> GetReasonAsync(Guid reasonId, CancellationToken cancellationToken);

    // Every parcel, shipment, bundle or dispatch with that number, exact numbers first, then parcels by their carrier's tracking number, newest first.
    Task<IReadOnlyList<(OperationsTrackingSubject Kind, Guid Id)>> FindSubjectsAsync(string number, CancellationToken cancellationToken);

    // Read-only, at most this many: the parcels of a shipment, bundle or dispatch for the history page.
    Task<IReadOnlyList<OperationsParcel>> ListSubjectParcelsAsync(OperationsTrackingSubject kind, Guid id, int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsParcel>> ListParcelsForUpdateAsync(IReadOnlyCollection<Guid> parcelIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsTrackingEvent>> ListCopiesAsync(Guid sourceEventId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsParcel>> ListShipmentParcelsForUpdateAsync(Guid shipmentId, CancellationToken cancellationToken);
}
