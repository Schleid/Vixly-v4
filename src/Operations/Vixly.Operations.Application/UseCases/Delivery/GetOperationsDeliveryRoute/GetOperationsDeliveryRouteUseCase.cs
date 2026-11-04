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

namespace Vixly.Operations.Application.UseCases.Delivery.GetOperationsDeliveryRoute;

// =========================================================
// FILE: GetOperationsDeliveryRouteUseCase.cs
//
// PURPOSE:
// A delivery route: its stops, what to collect, how each went; the shipments that may join it while planned (Slice 7 step 3, R-7, R-12, R-13, R-15).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.delivery.plan at its warehouse, or its own driver with ops.delivery.drive
// - A driver records on the phone page only when the company chose it (R-12)
// =========================================================
public sealed class GetOperationsDeliveryRouteUseCase
{
    private readonly OperationsDeliveryDesk _desk;

    private readonly IOperationsPickupRepository _pickups;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsDeliveryRouteUseCase(
        OperationsDeliveryDesk desk,
        IOperationsPickupRepository pickups,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _pickups = pickups ?? throw new ArgumentNullException(nameof(pickups));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsDeliveryRouteResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsDeliveryRouteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, OperationsDeliveryRouteView? view, bool planner, Error? refused) = await _desk.RouteForAsync(context, request.RouteId, cancellationToken);

        if (refused is not null || view is null)
        {
            return GetOperationsDeliveryRouteResponse.Failure(refused ?? OperationsDeliveryErrors.NotFound);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        OperationsDeliveryRoute route = view.Route;
        // A partner agent whose partner does not handle money sees no amounts (R-8).
        bool money = access!.HandlesMoneyAt(view.Warehouse);
        IReadOnlyList<OperationsDeliveryStopRow> stops = (await _desk.StopRowsAsync(view, settings, cancellationToken)).Select(stop => money ? stop : stop with { Due = 0m }).ToList();
        IReadOnlyList<OperationsDeliveryWaitingRow> waiting = Array.Empty<OperationsDeliveryWaitingRow>();

        if (planner && route.Status == OperationsRouteStatus.Planned)
        {
            waiting = (await _desk.WaitingAsync(view.Warehouse, settings, cancellationToken)).Select(item => new OperationsDeliveryWaitingRow(item.Shipment.Id, item.Shipment.Number ?? string.Empty,
                item.Shipment.RecipientName, item.Shipment.DeliveryAddress?.City, item.Shipment.DeliveryAddress?.PostalCode, item.Parcels.Count, item.Parcels.Max(parcel => parcel.DeliveryTries),
                money ? item.Due : 0m, item.CurrencyCode, OperationsDeliveryDesk.Blocked(item.Due, item.Waiting, settings, view.Warehouse))).ToList();
        }

        // The cash collected is paid in at the cashier: every invoice of a shipment that brought cash is paid (R-13).
        bool paidIn = !OperationsBundling.Unpaid(await _pickups.ListInvoicesAsync(view.Stops.Where(stop => stop.CashCollected > 0m).Select(stop => stop.ShipmentId).ToList(),
            cancellationToken));
        return GetOperationsDeliveryRouteResponse.Success(route.Id, route.Number, route.Status, route.ServiceDate, route.DriverName, view.Warehouse.Name, view.Warehouse.TimeZone ?? settings.TimeZone,
            planner, planner || settings.DeliveryRecording == OperationsDeliveryRecording.Phone, stops, waiting, route.CashCollected, route.CashHandedIn, route.SettleNote, route.CancelReason,
            paidIn, settings.DeliveryProofs, settings.DeliveryRecording, settings.CashOnDelivery, settings.CurrencyCode);
    }
}
