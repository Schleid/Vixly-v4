using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsTrackingRepository.cs
//
// PURPOSE:
// EF Core store of tracking events and packages (Slice 3A).
//
// RESPONSIBILITIES:
// - Add, find by key, list a subject's history
// - Include events added in this unit of work but not yet saved
//
// IMPORTANT NOTES:
// - Tenant-filtered by the model conventions (D-F1)
// =========================================================
internal sealed class OperationsTrackingRepository : IOperationsTrackingRepository
{
    private readonly OperationsDbContext _context;

    public OperationsTrackingRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsTrackingEvent trackingEvent) => _context.TrackingEvents.Add(trackingEvent ?? throw new ArgumentNullException(nameof(trackingEvent)));

    public void Add(OperationsParcel parcel) => _context.Parcels.Add(parcel ?? throw new ArgumentNullException(nameof(parcel)));

    public async Task<OperationsTrackingEvent?> FindByKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        _context.TrackingEvents.Local.FirstOrDefault(item => item.IdempotencyKey == idempotencyKey)
        ?? await _context.TrackingEvents.FirstOrDefaultAsync(item => item.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<OperationsTrackingEvent?> FindStoredByKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        _context.TrackingEvents.AsNoTracking().FirstOrDefaultAsync(item => item.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<OperationsTrackingEvent?> FindEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        await _context.TrackingEvents.FirstOrDefaultAsync(item => item.Id == eventId, cancellationToken);

    public async Task<IReadOnlyList<OperationsTrackingEvent>> ListForSubjectAsync(OperationsTrackingSubject kind, Guid subjectId, CancellationToken cancellationToken)
    {
        List<OperationsTrackingEvent> stored = await _context.TrackingEvents
            .Where(item => item.SubjectKind == kind && item.SubjectId == subjectId)
            .ToListAsync(cancellationToken);

        // Events recorded earlier in this unit of work count too, so several steps of one command project correctly.
        return stored
            .Concat(_context.TrackingEvents.Local.Where(item => item.SubjectKind == kind && item.SubjectId == subjectId))
            .DistinctBy(item => item.Id)
            .ToList();
    }

    // A package created earlier in this unit of work is found too (receiving creates it and records its first events together).
    public async Task<OperationsParcel?> GetParcelAsync(Guid packageId, CancellationToken cancellationToken) =>
        _context.Parcels.Local.FirstOrDefault(item => item.Id == packageId)
        ?? await _context.Parcels.FirstOrDefaultAsync(item => item.Id == packageId, cancellationToken);
}
