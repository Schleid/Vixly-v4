using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakeTrackingStore.cs
//
// PURPOSE:
// An in-memory tracking store for the use case tests (Slice 3A).
//
// RESPONSIBILITIES:
// - Keep events and packages; count adds
//
// IMPORTANT NOTES:
// - Test-only; one tenant per instance
// =========================================================
internal sealed class FakeTrackingStore : IOperationsTrackingRepository
{
    public List<OperationsTrackingEvent> Events { get; } = new();

    public List<OperationsParcel> Parcels { get; } = new();

    public void Add(OperationsTrackingEvent trackingEvent) => Events.Add(trackingEvent);

    public void Add(OperationsParcel parcel) => Parcels.Add(parcel);

    public Task<OperationsTrackingEvent?> FindByKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        Task.FromResult(Events.FirstOrDefault(item => item.IdempotencyKey == idempotencyKey));

    public Task<OperationsTrackingEvent?> FindStoredByKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        FindByKeyAsync(idempotencyKey, cancellationToken);

    public Task<OperationsTrackingEvent?> FindEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        Task.FromResult(Events.FirstOrDefault(item => item.Id == eventId));

    public Task<IReadOnlyList<OperationsTrackingEvent>> ListForSubjectAsync(OperationsTrackingSubject kind, Guid subjectId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsTrackingEvent>>(Events.Where(item => item.SubjectKind == kind && item.SubjectId == subjectId).ToList());

    public Task<IReadOnlyList<OperationsTrackingEvent>> ListForSubjectsAsync(OperationsTrackingSubject kind, IReadOnlyCollection<Guid> subjectIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsTrackingEvent>>(Events.Where(item => item.SubjectKind == kind && subjectIds.Contains(item.SubjectId)).ToList());

    public Task<IReadOnlyList<OperationsTrackingEvent>> FindByKeysAsync(IReadOnlyCollection<string> idempotencyKeys, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsTrackingEvent>>(Events.Where(item => item.IdempotencyKey is not null && idempotencyKeys.Contains(item.IdempotencyKey)).ToList());

    public Task<OperationsParcel?> GetParcelAsync(Guid packageId, CancellationToken cancellationToken) =>
        Task.FromResult(Parcels.FirstOrDefault(item => item.Id == packageId));
}
