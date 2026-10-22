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

namespace Vixly.Operations.Application.UseCases.Delivery.StartOperationsDeliveryRoute;

// =========================================================
// FILE: StartOperationsDeliveryRouteUseCase.cs
//
// PURPOSE:
// Send a planned route out: its parcels are out for delivery and the customers are told (Slice 7 step 3, R-7, R-15).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.delivery.plan at its warehouse, or its own driver
// - Starting twice does nothing more
// =========================================================
public sealed class StartOperationsDeliveryRouteUseCase
{
    private readonly OperationsDeliveryDesk _desk;

    private readonly IOperationsPickupRepository _pickups;

    private readonly IOperationsDeliveryRepository _routes;

    private readonly OperationsPickupCodes _codes;

    private readonly OperationsTracker _tracker;

    private readonly OperationsOutboxWriter _outbox;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public StartOperationsDeliveryRouteUseCase(
        OperationsDeliveryDesk desk,
        IOperationsPickupRepository pickups,
        IOperationsDeliveryRepository routes,
        OperationsPickupCodes codes,
        OperationsTracker tracker,
        OperationsOutboxWriter outbox,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _pickups = pickups ?? throw new ArgumentNullException(nameof(pickups));
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<StartOperationsDeliveryRouteResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        StartOperationsDeliveryRouteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, OperationsDeliveryRouteView? view, bool planner, Error? refused) = await _desk.RouteForAsync(context, request.RouteId, cancellationToken);

        if (refused is not null || view is null)
        {
            return StartOperationsDeliveryRouteResponse.Failure(refused ?? OperationsDeliveryErrors.NotFound);
        }

        OperationsDeliveryRoute route = view.Route;

        if (route.Status == OperationsRouteStatus.Out)
        {
            return StartOperationsDeliveryRouteResponse.Success();
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());

        // Paid unless cash is collected at the door, and no payment waiting (R-13): checked again as it leaves.
        foreach (OperationsDeliveryStop stop in view.Stops)
        {
            (decimal due, bool waiting, _) = await _desk.DueAsync(stop.ShipmentId, cancellationToken);

            if (OperationsDeliveryDesk.Blocked(due, waiting, settings, view.Warehouse) || view.ParcelsOf(stop.ShipmentId).Count == 0
                || !view.ParcelsOf(stop.ShipmentId).All(OperationsDeliveryDesk.Deliverable))
            {
                return StartOperationsDeliveryRouteResponse.Failure(OperationsDeliveryErrors.NotDeliverable);
            }
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            route.Start(utcNow);
        }
        catch (DomainException exception)
        {
            return StartOperationsDeliveryRouteResponse.Failure(exception.Error);
        }

        if (await _tracker.RecordManyAsync(view.Parcels.Select(parcel => (new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.OutForDelivery,
            new Dictionary<string, string>(StringComparer.Ordinal), utcNow, "out:" + route.Id.ToString("N") + ":" + parcel.Id.ToString("N"), OperationsTrackingActor.Staff,
            OperationsTrackingSource.Manual) { ActorUserId = access!.User.Id }, parcel)).ToList(), cancellationToken) is { } failed)
        {
            _unitOfWork.DiscardChanges();
            return StartOperationsDeliveryRouteResponse.Failure(failed);
        }

        bool withCode = settings.DeliveryProofs.HasFlag(OperationsDeliveryProofs.Code);

        // The delivery code is made as the route leaves, whether or not the email can be sent (review); the email gives the one still valid.
        if (withCode)
        {
            foreach (OperationsShipment shipment in view.Shipments.Values.Where(item => _codes.Current(item) is null))
            {
                _codes.Issue(shipment);
            }
        }

        if ((settings.DeliveryEmails || withCode) && !string.IsNullOrWhiteSpace(request.PortalSlug))
        {
            foreach (OperationsShipment shipment in view.Shipments.Values)
            {
                _outbox.Write(OperationsOutForDeliveryNotice.MessageType, route.TenantId, shipment.Id, new OperationsOutForDeliveryNotice(shipment.CustomerId, request.PortalSlug,
                    shipment.Number ?? string.Empty, withCode));
            }
        }

        _audit.Record(access!.User.Id, "ops.delivery.route_started", nameof(OperationsDeliveryRoute), route.Id, after: new { route.Number, stops = route.StopCount, parcels = view.Parcels.Count });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return StartOperationsDeliveryRouteResponse.Success();
        }

        // A double click: started by the first.
        _unitOfWork.DiscardChanges();
        return await _routes.FindByIdAsync(route.Id, cancellationToken) is { Status: OperationsRouteStatus.Out }
            ? StartOperationsDeliveryRouteResponse.Success()
            : StartOperationsDeliveryRouteResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
