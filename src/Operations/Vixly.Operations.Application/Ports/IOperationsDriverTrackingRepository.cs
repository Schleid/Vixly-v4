using Vixly.Operations.Domain.Delivery;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsDriverTrackingRepository.cs
//
// PURPOSE:
// Driver positions, and the stops a customer may follow (Slice 12C, owner H-5).
//
// RESPONSIBILITIES:
// - Add a position; the latest and the recent ones of a route
// - Find the stop of a shipment on a route that is out, or the stop of a private link
// - Delete positions past their keeping time
//
// IMPORTANT NOTES:
// - Scoped to the tenant by the data layer, except the clean-up (system scope)
// =========================================================
public interface IOperationsDriverTrackingRepository
{
    void Add(OperationsDriverPosition position);

    Task<OperationsDriverPosition?> LatestAsync(Guid routeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsDriverPosition>> ListSinceAsync(Guid routeId, DateTimeOffset sinceUtc, int take, CancellationToken cancellationToken);

    // The stop of the shipment on a route that is out (tracked), with that route.
    Task<(OperationsDeliveryStop Stop, OperationsDeliveryRoute Route)?> FindOutStopAsync(Guid shipmentId, CancellationToken cancellationToken);

    // The stop of a private link, with its route (not tracked).
    Task<(OperationsDeliveryStop Stop, OperationsDeliveryRoute Route)?> FindByTrackTokenAsync(string tokenHash, CancellationToken cancellationToken);

    // How many stops of the route come before this one and are still to do.
    Task<int> StopsBeforeAsync(Guid routeId, int sequence, CancellationToken cancellationToken);

    Task<int> DeleteOlderThanAsync(DateTimeOffset cutoffUtc, int batchSize, CancellationToken cancellationToken);
}
