using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Delivery;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.PlanOperationsDeliveryRoute;

// =========================================================
// FILE: PlanOperationsDeliveryRouteUseCase.cs
//
// PURPOSE:
// Plan a driver's route from a warehouse with shipments waiting for delivery (Slice 7 step 3, R-7, R-12).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.delivery.plan at the warehouse
// - Only shipments waiting there, paid unless the company takes cash on delivery (R-13)
// - One request key: a double click plans once
// =========================================================
public sealed class PlanOperationsDeliveryRouteUseCase
{
    private readonly OperationsDeliveryDesk _desk;

    private readonly IOperationsDeliveryRepository _routes;

    private readonly OperationsNumbering _numbering;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PlanOperationsDeliveryRouteUseCase(
        OperationsDeliveryDesk desk,
        IOperationsDeliveryRepository routes,
        OperationsNumbering numbering,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PlanOperationsDeliveryRouteResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        PlanOperationsDeliveryRouteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.DeliveryPlan, cancellationToken);

        if (refused is not null)
        {
            return PlanOperationsDeliveryRouteResponse.Failure(refused);
        }

        // A double click: the route the first click made (ADR-0019).
        if (request.RequestKey is { } key && await _routes.FindByKeyAsync(key, cancellationToken) is { } made && made.WarehouseId == request.WarehouseId)
        {
            return PlanOperationsDeliveryRouteResponse.Success(made.Id);
        }

        if (warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) is not { } here)
        {
            return PlanOperationsDeliveryRouteResponse.Failure(OperationsDeliveryErrors.NotFound);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        string? driverName = request.DriverName;

        // The phone page needs a driver who signs in (R-12); the run sheet may name anyone.
        if (request.DriverUserId is { } driverId)
        {
            if ((await _desk.DriversAsync(here, cancellationToken)).FirstOrDefault(item => item.UserId == driverId) is not { } driver)
            {
                return PlanOperationsDeliveryRouteResponse.Failure(OperationsDeliveryErrors.DriverRequired);
            }

            driverName = driver.Name;
        }
        else if (settings.DeliveryRecording == OperationsDeliveryRecording.Phone)
        {
            return PlanOperationsDeliveryRouteResponse.Failure(OperationsDeliveryErrors.DriverRequired);
        }

        List<Guid> wanted = request.ShipmentIds.Distinct().ToList();

        if (wanted.Count == 0)
        {
            return PlanOperationsDeliveryRouteResponse.Failure(OperationsDeliveryErrors.Empty);
        }

        if (wanted.Count > OperationsDeliveryRoute.MaxStops)
        {
            return PlanOperationsDeliveryRouteResponse.Failure(OperationsDeliveryErrors.TooManyStops);
        }

        Dictionary<Guid, (OperationsShipment Shipment, IReadOnlyList<OperationsParcel> Parcels, decimal Due, bool Waiting, string? CurrencyCode)> waiting =
            (await _desk.WaitingAsync(here, settings, cancellationToken)).ToDictionary(item => item.Shipment.Id);

        if (wanted.Any(id => !waiting.TryGetValue(id, out var item) || OperationsDeliveryDesk.Blocked(item.Due, item.Waiting, settings, here)))
        {
            return PlanOperationsDeliveryRouteResponse.Failure(OperationsDeliveryErrors.NotDeliverable);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsDeliveryRoute route;

        try
        {
            // Checked before a number is taken.
            route = OperationsDeliveryRoute.Plan(context.RequiredTenantId, "pending", here.Id, request.ServiceDate, request.DriverUserId, driverName, access!.User.Id, request.RequestKey,
                OperationsTimeZones.Today(utcNow, here.TimeZone ?? settings.TimeZone), utcNow);
            route = OperationsDeliveryRoute.Plan(context.RequiredTenantId, await _numbering.NextAsync(OperationsNumberKind.Route, cancellationToken), here.Id, request.ServiceDate,
                request.DriverUserId, driverName, access.User.Id, request.RequestKey, OperationsTimeZones.Today(utcNow, here.TimeZone ?? settings.TimeZone), utcNow);
            _routes.Add(route);
            int sequence = 0;

            foreach (Guid shipmentId in wanted)
            {
                IReadOnlyList<OperationsParcel> parcels = waiting[shipmentId].Parcels;
                _routes.AddStop(OperationsDeliveryStop.Create(context.RequiredTenantId, route.Id, shipmentId, ++sequence, parcels.Count, utcNow));

                foreach (OperationsParcel parcel in parcels)
                {
                    parcel.PutOnRoute(route.Id, utcNow);
                }
            }

            route.StopsChanged(sequence, utcNow);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return PlanOperationsDeliveryRouteResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, "ops.delivery.route_planned", nameof(OperationsDeliveryRoute), route.Id, after: new { route.Number, route.ServiceDate, route.DriverName, stops = route.StopCount });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return PlanOperationsDeliveryRouteResponse.Success(route.Id);
        }

        _unitOfWork.DiscardChanges();
        return request.RequestKey is { } again && await _routes.FindByKeyAsync(again, cancellationToken) is { } other
            ? PlanOperationsDeliveryRouteResponse.Success(other.Id)
            : PlanOperationsDeliveryRouteResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
