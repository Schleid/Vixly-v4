using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Events;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsEventRepository.cs
//
// PURPOSE:
// EF Core access for operational events (Slice 6C).
//
// RESPONSIBILITIES:
// - Implement IOperationsEventRepository
//
// IMPORTANT NOTES:
// - Tenant-scoped by the context's query filter
// =========================================================
internal sealed class OperationsEventRepository : IOperationsEventRepository
{
    private readonly OperationsDbContext _context;

    public OperationsEventRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsEventReason reason) => _context.EventReasons.Add(reason ?? throw new ArgumentNullException(nameof(reason)));

    public async Task<IReadOnlyList<OperationsEventReason>> ListReasonsAsync(CancellationToken cancellationToken) =>
        await _context.EventReasons.AsNoTracking().Take(OperationsEventReason.MaxPerCompany * 2).ToListAsync(cancellationToken);

    public Task<OperationsEventReason?> GetReasonAsync(Guid reasonId, CancellationToken cancellationToken) =>
        _context.EventReasons.FirstOrDefaultAsync(reason => reason.Id == reasonId, cancellationToken);

    public async Task<IReadOnlyList<(OperationsTrackingSubject Kind, Guid Id)>> FindSubjectsAsync(string number, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        List<(OperationsTrackingSubject, Guid)> found = new();
        found.AddRange((await _context.Parcels.AsNoTracking().Where(parcel => parcel.Number == number).Select(parcel => parcel.Id).Take(5).ToListAsync(cancellationToken))
            .Select(id => (OperationsTrackingSubject.Parcel, id)));
        found.AddRange((await _context.Shipments.AsNoTracking().Where(shipment => shipment.Number == number).Select(shipment => shipment.Id).Take(5).ToListAsync(cancellationToken))
            .Select(id => (OperationsTrackingSubject.Shipment, id)));
        found.AddRange((await _context.Bundles.AsNoTracking().Where(bundle => bundle.Number == number).Select(bundle => bundle.Id).Take(5).ToListAsync(cancellationToken))
            .Select(id => (OperationsTrackingSubject.Bundle, id)));
        found.AddRange((await _context.Dispatches.AsNoTracking().Where(dispatch => dispatch.Number == number).Select(dispatch => dispatch.Id).Take(5).ToListAsync(cancellationToken))
            .Select(id => (OperationsTrackingSubject.Dispatch, id)));
        found.AddRange((await _context.Parcels.AsNoTracking().Where(parcel => parcel.CarrierTrackingNumber == number).OrderByDescending(parcel => parcel.CreatedUtc)
            .Select(parcel => parcel.Id).Take(20).ToListAsync(cancellationToken)).Select(id => (OperationsTrackingSubject.Parcel, id)));
        return found.Distinct().ToList();
    }

    public async Task<IReadOnlyList<OperationsParcel>> ListSubjectParcelsAsync(OperationsTrackingSubject kind, Guid id, int take, CancellationToken cancellationToken)
    {
        IQueryable<OperationsParcel> parcels = _context.Parcels.AsNoTracking();
        parcels = kind switch
        {
            OperationsTrackingSubject.Shipment => parcels.Where(parcel => parcel.ShipmentId == id),
            OperationsTrackingSubject.Bundle => parcels.Where(parcel => parcel.BundleId == id),
            OperationsTrackingSubject.Dispatch => parcels.Where(parcel => _context.Bundles.Any(bundle => bundle.Id == parcel.BundleId && bundle.DispatchId == id)),
            _ => parcels.Where(parcel => parcel.Id == id)
        };
        return await parcels.OrderBy(parcel => parcel.CreatedUtc).Take(take).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsParcel>> ListParcelsForUpdateAsync(IReadOnlyCollection<Guid> parcelIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcelIds);
        List<OperationsParcel> found = new();

        foreach (Guid[] slice in parcelIds.Distinct().Chunk(1000))
        {
            found.AddRange(await _context.Parcels.Where(parcel => slice.Contains(parcel.Id)).ToListAsync(cancellationToken));
        }

        return found;
    }

    public async Task<IReadOnlyList<OperationsTrackingEvent>> ListCopiesAsync(Guid sourceEventId, CancellationToken cancellationToken) =>
        await _context.TrackingEvents.AsNoTracking().Where(item => item.CausedByEventId == sourceEventId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsParcel>> ListShipmentParcelsForUpdateAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        await _context.Parcels.Where(parcel => parcel.ShipmentId == shipmentId).OrderBy(parcel => parcel.CreatedUtc).ToListAsync(cancellationToken);
}
