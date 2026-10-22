using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Delivery;
using Vixly.Operations.Application.Events;
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

namespace Vixly.Operations.Application.UseCases.Delivery.GetMyOperationsDeliveryRoutes;

// =========================================================
// FILE: GetMyOperationsDeliveryRoutesUseCase.cs
//
// PURPOSE:
// A driver's own routes, from yesterday on (Slice 7 step 3, R-12).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.delivery.drive at the route's warehouse
// =========================================================
public sealed class GetMyOperationsDeliveryRoutesUseCase
{
    private readonly OperationsDeliveryDesk _desk;

    private readonly IOperationsDeliveryRepository _routes;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetMyOperationsDeliveryRoutesUseCase(
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

    public async Task<GetMyOperationsDeliveryRoutesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsDeliveryRoutesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.DeliveryDrive, cancellationToken);

        if (refused is not null)
        {
            return GetMyOperationsDeliveryRoutesResponse.Failure(refused);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        DateOnly today = OperationsTimeZones.Today(_timeProvider.GetUtcNow(), settings.TimeZone);
        HashSet<Guid> mine = warehouses.Select(item => item.Id).ToHashSet();
        List<OperationsDeliveryRoute> routes = (await _routes.ListDriverRoutesAsync(access!.User.Id, today.AddDays(-1), 50, cancellationToken))
            .Where(route => mine.Contains(route.WarehouseId)).ToList();
        ILookup<Guid, OperationsDeliveryStop> stops = (await _routes.ListStopsAsync(routes.Select(route => route.Id).ToList(), cancellationToken)).ToLookup(stop => stop.RouteId);
        return GetMyOperationsDeliveryRoutesResponse.Success(routes.Select(route => Row(route, stops[route.Id])).ToList());
    }

    private static OperationsDeliveryRouteRow Row(OperationsDeliveryRoute route, IEnumerable<OperationsDeliveryStop> stops) =>
        new(route.Id, route.Number, route.ServiceDate, route.DriverName, route.Status, route.StopCount, stops.Count(stop => stop.IsDone), route.CashCollected);
}
