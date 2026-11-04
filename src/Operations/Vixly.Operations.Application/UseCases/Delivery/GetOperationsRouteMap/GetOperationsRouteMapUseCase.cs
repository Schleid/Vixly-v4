using Vixly.Operations.Application.Delivery;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Delivery;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.GetOperationsRouteMap;

// =========================================================
// FILE: GetOperationsRouteMapUseCase.cs
//
// PURPOSE:
// The driver's latest place and today's way on the route page (Slice 12C, owner H-5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Seen by the route's planners and its driver, as the route itself
// =========================================================
public sealed class GetOperationsRouteMapUseCase
{
    private readonly OperationsDeliveryDesk _desk;

    private readonly IOperationsDriverTrackingRepository _tracking;

    private readonly TimeProvider _timeProvider;

    public GetOperationsRouteMapUseCase(
        OperationsDeliveryDesk desk,
        IOperationsDriverTrackingRepository tracking,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsRouteMapResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsRouteMapRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, OperationsDeliveryRouteView? view, bool _, Error? refused) = await _desk.RouteForAsync(context, request.RouteId, cancellationToken);

        if (refused is not null || view is null)
        {
            return GetOperationsRouteMapResponse.Failure(refused ?? OperationsDeliveryErrors.NotFound);
        }

        IReadOnlyList<OperationsDriverPosition> positions = await _tracking.ListSinceAsync(view.Route.Id, _timeProvider.GetUtcNow().AddHours(-12), 500, cancellationToken);
        OperationsDeliveryStop? next = view.Stops.Where(stop => !stop.IsDone).OrderBy(stop => stop.Sequence).FirstOrDefault();
        Vixly.Operations.Domain.Shipments.OperationsShipment? shipment = next is null ? null : view.Shipments.GetValueOrDefault(next.ShipmentId);

        return GetOperationsRouteMapResponse.Success(new OperationsRouteMap(
            positions.Select(item => new OperationsRouteMapPoint(item.Latitude, item.Longitude, item.AccuracyMeters, item.TakenUtc)).ToList(),
            shipment?.Number, next is null ? null : (shipment is null ? null : OperationsDeliveryDesk.AddressLine(shipment))));
    }
}
