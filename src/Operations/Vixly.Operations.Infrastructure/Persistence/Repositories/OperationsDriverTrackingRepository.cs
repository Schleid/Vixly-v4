using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Delivery;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsDriverTrackingRepository.cs
//
// PURPOSE:
// Driver positions and followed stops (Slice 12C).
//
// RESPONSIBILITIES:
// - Implement IOperationsDriverTrackingRepository with EF Core
//
// IMPORTANT NOTES:
// - Tenant scoping comes from the context's query filters; the clean-up runs in the system scope
// =========================================================
internal sealed class OperationsDriverTrackingRepository : IOperationsDriverTrackingRepository
{
    private readonly OperationsDbContext _context;

    public OperationsDriverTrackingRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsDriverPosition position) => _context.DriverPositions.Add(position ?? throw new ArgumentNullException(nameof(position)));

    public Task<OperationsDriverPosition?> LatestAsync(Guid routeId, CancellationToken cancellationToken) =>
        _context.DriverPositions.AsNoTracking().Where(item => item.RouteId == routeId).OrderByDescending(item => item.TakenUtc).ThenByDescending(item => item.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsDriverPosition>> ListSinceAsync(Guid routeId, DateTimeOffset sinceUtc, int take, CancellationToken cancellationToken) =>
        (await _context.DriverPositions.AsNoTracking().Where(item => item.RouteId == routeId && item.TakenUtc >= sinceUtc).OrderByDescending(item => item.TakenUtc).Take(take)
            .ToListAsync(cancellationToken)).OrderBy(item => item.TakenUtc).ToList();

    public async Task<(OperationsDeliveryStop Stop, OperationsDeliveryRoute Route)?> FindOutStopAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        var found = await (from stop in _context.DeliveryStops
                           join route in _context.DeliveryRoutes on stop.RouteId equals route.Id
                           where stop.ShipmentId == shipmentId && route.Status == OperationsRouteStatus.Out
                           orderby route.StartedUtc descending
                           select new { stop, route }).FirstOrDefaultAsync(cancellationToken);
        return found is null ? null : (found.stop, found.route);
    }

    public async Task<(OperationsDeliveryStop Stop, OperationsDeliveryRoute Route)?> FindByTrackTokenAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var found = await (from stop in _context.DeliveryStops.AsNoTracking()
                           join route in _context.DeliveryRoutes.AsNoTracking() on stop.RouteId equals route.Id
                           where stop.TrackTokenHash == tokenHash
                           select new { stop, route }).FirstOrDefaultAsync(cancellationToken);
        return found is null ? null : (found.stop, found.route);
    }

    public Task<int> StopsBeforeAsync(Guid routeId, int sequence, CancellationToken cancellationToken) =>
        _context.DeliveryStops.CountAsync(item => item.RouteId == routeId && item.Sequence < sequence && item.Status == OperationsStopStatus.Pending, cancellationToken);

    public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoffUtc, int batchSize, CancellationToken cancellationToken) =>
        _context.DriverPositions.Where(item => item.TakenUtc < cutoffUtc).OrderBy(item => item.TakenUtc).Take(batchSize).ExecuteDeleteAsync(cancellationToken);
}
