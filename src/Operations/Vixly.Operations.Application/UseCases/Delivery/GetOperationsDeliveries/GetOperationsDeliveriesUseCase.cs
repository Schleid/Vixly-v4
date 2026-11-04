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

namespace Vixly.Operations.Application.UseCases.Delivery.GetOperationsDeliveries;

// =========================================================
// FILE: GetOperationsDeliveriesUseCase.cs
//
// PURPOSE:
// The delivery desk of a warehouse: shipments waiting for delivery, its routes, parcels going back (Slice 7 step 3, R-7, R-14).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.delivery.plan, per warehouse
// =========================================================
public sealed class GetOperationsDeliveriesUseCase
{
    private readonly OperationsDeliveryDesk _desk;

    private readonly IOperationsDeliveryRepository _routes;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsDeliveriesUseCase(
        OperationsDeliveryDesk desk,
        IOperationsDeliveryRepository routes,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsDeliveriesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsDeliveriesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.DeliveryPlan, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsDeliveriesResponse.Failure(refused);
        }

        OperationsWarehouse here = warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) ?? warehouses[0];
        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        DateOnly today = OperationsTimeZones.Today(_timeProvider.GetUtcNow(), here.TimeZone ?? settings.TimeZone);
        IReadOnlyList<OperationsDeliveryRoute> routes = await _routes.ListRoutesAsync(here.Id, today.AddDays(-7), 100, cancellationToken);
        ILookup<Guid, OperationsDeliveryStop> stops = (await _routes.ListStopsAsync(routes.Select(route => route.Id).ToList(), cancellationToken)).ToLookup(stop => stop.RouteId);
        IReadOnlyList<(OperationsShipment Shipment, IReadOnlyList<OperationsParcel> Parcels, decimal Due, bool Waiting, string? CurrencyCode)> waiting =
            await _desk.WaitingAsync(here, settings, cancellationToken);
        // A partner agent whose partner does not handle money sees no amounts (R-8).
        bool money = access!.HandlesMoneyAt(here);
        IReadOnlyList<(OperationsParcel Parcel, OperationsShipment Shipment)> returns = await _routes.ListReturnsAsync(here.Id, 200, cancellationToken);
        return GetOperationsDeliveriesResponse.Success(warehouses.Select(item => new OperationsArrivalPlace(item.Id, item.Code, item.Name, item.ClearsCustoms) { TimeZone = item.TimeZone }).ToList(),
            here.Id, today,
            waiting.Select(item => new OperationsDeliveryWaitingRow(item.Shipment.Id, item.Shipment.Number ?? string.Empty, item.Shipment.RecipientName, item.Shipment.DeliveryAddress?.City,
                item.Shipment.DeliveryAddress?.PostalCode, item.Parcels.Count, item.Parcels.Max(parcel => parcel.DeliveryTries), money ? item.Due : 0m, item.CurrencyCode,
                OperationsDeliveryDesk.Blocked(item.Due, item.Waiting, settings, here))).ToList(),
            routes.Select(route => Row(route, stops[route.Id])).ToList(),
            returns.Select(item => new OperationsDeliveryReturnRow(item.Parcel.Id, item.Parcel.Number ?? string.Empty, item.Shipment.Id, item.Shipment.Number ?? string.Empty, item.Shipment.RecipientName,
                item.Parcel.DeliveryTries)).ToList(),
            await _desk.DriversAsync(here, cancellationToken), settings.DeliveryRecording, settings.CashOnDelivery,
            settings.DestinationCharges);
    }

    private static OperationsDeliveryRouteRow Row(OperationsDeliveryRoute route, IEnumerable<OperationsDeliveryStop> stops) =>
        new(route.Id, route.Number, route.ServiceDate, route.DriverName, route.Status, route.StopCount, stops.Count(stop => stop.IsDone), route.CashCollected);
}
