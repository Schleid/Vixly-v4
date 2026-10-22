using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Delivery;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.ChangeOperationsDeliveryRoute;

// =========================================================
// FILE: ChangeOperationsDeliveryRouteUseCase.cs
//
// PURPOSE:
// Change a planned route: add shipments waiting, take a stop off, move a stop up or down (Slice 7 step 3, R-7).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.delivery.plan at its warehouse
// - Planned routes only; stops are numbered again from 1
// =========================================================
public sealed class ChangeOperationsDeliveryRouteUseCase
{
    private readonly OperationsDeliveryDesk _desk;

    private readonly IOperationsDeliveryRepository _routes;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsDeliveryRouteUseCase(
        OperationsDeliveryDesk desk,
        IOperationsDeliveryRepository routes,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsDeliveryRouteResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsDeliveryRouteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, OperationsDeliveryRouteView? view, bool planner, Error? refused) = await _desk.RouteForAsync(context, request.RouteId, cancellationToken);

        if (refused is not null || view is null)
        {
            return ChangeOperationsDeliveryRouteResponse.Failure(refused ?? OperationsDeliveryErrors.NotFound);
        }

        if (!planner)
        {
            return ChangeOperationsDeliveryRouteResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        OperationsDeliveryRoute route = view.Route;

        if (route.Status != OperationsRouteStatus.Planned)
        {
            return ChangeOperationsDeliveryRouteResponse.Failure(OperationsDeliveryErrors.NotPlanned);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        List<OperationsDeliveryStop> stops = view.Stops.OrderBy(stop => stop.Sequence).ToList();
        object change;

        try
        {
            if (request.RemoveStopId is { } removeId)
            {
                if (stops.FirstOrDefault(stop => stop.Id == removeId) is not { } removed)
                {
                    return ChangeOperationsDeliveryRouteResponse.Failure(OperationsDeliveryErrors.NotFound);
                }

                foreach (OperationsParcel parcel in view.ParcelsOf(removed.ShipmentId))
                {
                    parcel.OffRoute(false, utcNow);
                }

                _routes.RemoveStop(removed);
                stops.Remove(removed);
                change = new { removed = view.Shipments.GetValueOrDefault(removed.ShipmentId)?.Number };
            }
            else if (request.MoveStopId is { } moveId)
            {
                int at = stops.FindIndex(stop => stop.Id == moveId);
                int to = request.Up ? at - 1 : at + 1;

                if (at < 0)
                {
                    return ChangeOperationsDeliveryRouteResponse.Failure(OperationsDeliveryErrors.NotFound);
                }

                if (to >= 0 && to < stops.Count)
                {
                    (stops[at], stops[to]) = (stops[to], stops[at]);
                }

                change = new { moved = moveId, up = request.Up };
            }
            else
            {
                List<Guid> wanted = request.AddShipmentIds.Distinct().Where(id => stops.All(stop => stop.ShipmentId != id)).ToList();
                Dictionary<Guid, (OperationsShipment Shipment, IReadOnlyList<OperationsParcel> Parcels, decimal Due, bool Waiting, string? CurrencyCode)> waiting =
                    (await _desk.WaitingAsync(view.Warehouse, settings, cancellationToken)).ToDictionary(item => item.Shipment.Id);

                if (wanted.Count == 0 || wanted.Any(id => !waiting.TryGetValue(id, out var item) || OperationsDeliveryDesk.Blocked(item.Due, item.Waiting, settings, view.Warehouse)))
                {
                    return ChangeOperationsDeliveryRouteResponse.Failure(wanted.Count == 0 ? OperationsDeliveryErrors.Empty : OperationsDeliveryErrors.NotDeliverable);
                }

                if (stops.Count + wanted.Count > OperationsDeliveryRoute.MaxStops)
                {
                    return ChangeOperationsDeliveryRouteResponse.Failure(OperationsDeliveryErrors.TooManyStops);
                }

                foreach (Guid shipmentId in wanted)
                {
                    OperationsDeliveryStop stop = OperationsDeliveryStop.Create(context.RequiredTenantId, route.Id, shipmentId, stops.Count + 1, waiting[shipmentId].Parcels.Count, utcNow);
                    _routes.AddStop(stop);
                    stops.Add(stop);

                    foreach (OperationsParcel parcel in waiting[shipmentId].Parcels)
                    {
                        parcel.PutOnRoute(route.Id, utcNow);
                    }
                }

                change = new { added = wanted.Count };
            }

            for (int index = 0; index < stops.Count; index++)
            {
                if (stops[index].Sequence != index + 1)
                {
                    stops[index].Renumber(index + 1, utcNow);
                }
            }

            route.StopsChanged(stops.Count, utcNow);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return ChangeOperationsDeliveryRouteResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.delivery.route_changed", nameof(OperationsDeliveryRoute), route.Id, after: change);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ChangeOperationsDeliveryRouteResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return ChangeOperationsDeliveryRouteResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
