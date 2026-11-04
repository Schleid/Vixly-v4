using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsTrackingRepository.cs
//
// PURPOSE:
// Stores tracking events and the packages they describe (Slice 3A, ADR-0021).
//
// RESPONSIBILITIES:
// - Add events and packages; find an event by its idempotency key
// - List a subject's events, including those added but not yet saved
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1); events are never updated or deleted
// =========================================================
public interface IOperationsTrackingRepository
{
    void Add(OperationsTrackingEvent trackingEvent);

    void Add(OperationsParcel parcel);

    Task<OperationsTrackingEvent?> FindByKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    // The saved event with this key, ignoring anything only added in this unit of work.
    Task<OperationsTrackingEvent?> FindStoredByKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task<OperationsTrackingEvent?> FindEventAsync(Guid eventId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsTrackingEvent>> ListForSubjectAsync(OperationsTrackingSubject kind, Guid subjectId, CancellationToken cancellationToken);

    // Slice 6B: many subjects at once (a dispatch's parcels), with the events of this unit of work.
    Task<IReadOnlyList<OperationsTrackingEvent>> ListForSubjectsAsync(OperationsTrackingSubject kind, IReadOnlyCollection<Guid> subjectIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsTrackingEvent>> FindByKeysAsync(IReadOnlyCollection<string> idempotencyKeys, CancellationToken cancellationToken);

    Task<OperationsParcel?> GetParcelAsync(Guid packageId, CancellationToken cancellationToken);
}
