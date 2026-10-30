using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.Delivery;

// =========================================================
// FILE: OperationsDeliveryDesk.cs
//
// PURPOSE:
// What every home-delivery use case shares (Slice 7 step 3, R-7, R-12 to R-15).
//
// RESPONSIBILITIES:
// - Open the delivery desk for the warehouses where the person plans routes (or drives)
// - The people who may drive from a warehouse
// - The shipments waiting for delivery at a warehouse, and what is owed for each
// - Load a route with its stops, shipments and parcels
//
// IMPORTANT NOTES:
// - Waiting means: at the warehouse its shipment ends at, arrived there, for home delivery, not held by customs, cleared when the warehouse clears customs,
//   on no route, not going back, fewer tries than the company allows
// - Owed: the unpaid invoices of the shipment; a payment awaiting confirmation or credit blocks the delivery
// =========================================================
public sealed class OperationsDeliveryDesk
{
    private readonly OperationsBundleDesk _bundles;
    private readonly IOperationsDeliveryRepository _routes;
    private readonly IOperationsPickupRepository _pickups;
    private readonly IOperationsTrackingRepository _tracking;
    private readonly IOperationsLocationRepository _locations;
    private readonly IOperationsUserRepository _users;
    private readonly IOperationsRoleRepository _roles;

    private readonly OperationsAccess _access;

    public OperationsDeliveryDesk(OperationsBundleDesk bundles, IOperationsDeliveryRepository routes, IOperationsPickupRepository pickups, IOperationsTrackingRepository tracking,
        IOperationsLocationRepository locations, IOperationsUserRepository users, IOperationsRoleRepository roles, OperationsAccess access)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        _pickups = pickups ?? throw new ArgumentNullException(nameof(pickups));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
    }

    public Task<(OperationsAccessView? Access, IReadOnlyList<OperationsWarehouse> Warehouses, Error? Refused)> OpenAsync(OperationsAuthorizationContext context, string permission,
        CancellationToken cancellationToken) =>
        _bundles.OpenAsync(context, new[] { permission }, cancellationToken);

    // Active people whose role (not the administrator's) lets them drive from this warehouse, by name.
    public async Task<IReadOnlyList<OperationsDeliveryDriver>> DriversAsync(OperationsWarehouse here, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(here);
        Dictionary<Guid, OperationsRole> roles = (await _roles.ListAsync(cancellationToken)).Where(role => !role.IsAdministrator && role.Holds(OperationsPermissions.DeliveryDrive))
            .ToDictionary(role => role.Id);
        List<Guid> userIds = (await _roles.ListActiveGrantsAsync(cancellationToken)).Where(grant => roles.ContainsKey(grant.RoleId) && grant.Scope switch
        {
            OperationsRoleScope.Tenant => true,
            OperationsRoleScope.Branch => grant.ScopeId == here.BranchId,
            _ => grant.ScopeId == here.Id
        }).Select(grant => grant.UserId).Distinct().Take(200).ToList();
        List<OperationsDeliveryDriver> drivers = new();

        // The partner's own staff drive from the partner's warehouse (R-8), while partner access is open.
        if (here.OperatedBy == OperationsDestinationOperator.Partner
            && (await _access.PartnerScopeAsync(OperationsAuthorizationContext.ForTenant(here.TenantId), here.Id, cancellationToken)).Open)
        {
            userIds.AddRange((await _users.ListPartnerAgentsAsync(here.Id, cancellationToken)).Select(user => user.Id).Where(id => !userIds.Contains(id)));
        }

        foreach (Guid userId in userIds)
        {
            if (await _users.GetAsync(userId, cancellationToken) is { Account.IsActive: true } user)
            {
                drivers.Add(new OperationsDeliveryDriver(user.Id, user.FullName));
            }
        }

        return drivers.OrderBy(driver => driver.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    // What the customer owes on the shipment's invoices, and whether a payment waits for a decision (it cannot go out then).
    // Invoices already collected for at the door (on a route not settled) are not owed again (review).
    public static (decimal Due, bool Waiting, string? CurrencyCode) Owed(IEnumerable<OperationsInvoice> invoices, IReadOnlySet<Guid>? collected = null)
    {
        List<OperationsInvoice> all = (invoices ?? throw new ArgumentNullException(nameof(invoices))).ToList();
        List<OperationsInvoice> unpaid = all.Where(invoice => invoice.PaymentState == OperationsPaymentState.Unpaid && collected?.Contains(invoice.Id) != true).ToList();
        return (unpaid.Sum(invoice => invoice.Owed), all.Any(invoice => invoice.PaymentState is OperationsPaymentState.AwaitingConfirmation or OperationsPaymentState.AwaitingCredit),
            unpaid.FirstOrDefault()?.CurrencyCode ?? all.FirstOrDefault()?.CurrencyCode);
    }

    // Unpaid with cash on delivery off, or a payment waiting: it waits here.
    // Cash on delivery only where money is handled: never from a partner's warehouse the company does not let take payments (R-8).
    public static bool Blocked(decimal due, bool waiting, OperationsTenantSettings settings, OperationsWarehouse here) =>
        waiting || (due > 0m && !((settings ?? throw new ArgumentNullException(nameof(settings))).CashOnDelivery && (here ?? throw new ArgumentNullException(nameof(here))).HandlesMoney));

    public static string AddressLine(OperationsShipment shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        return shipment.DeliveryAddress is not { } address ? string.Empty
            : string.Join(", ", new[] { address.Line1, address.Line2, address.PostalCode is null ? address.City : address.PostalCode + " " + address.City, address.Region }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    // The shipments waiting for delivery here: each with its parcels waiting, oldest first.
    public async Task<IReadOnlyList<(OperationsShipment Shipment, IReadOnlyList<OperationsParcel> Parcels, decimal Due, bool Waiting, string? CurrencyCode)>> WaitingAsync(
        OperationsWarehouse here, OperationsTenantSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(here);
        ArgumentNullException.ThrowIfNull(settings);
        List<(OperationsShipment Shipment, IReadOnlyList<OperationsParcel> Parcels)> found = (await _routes.ListWaitingAsync(here.Id, 300, cancellationToken))
            .Where(item => OperationsArrivalDesk.EndsAt(here, OperationsBundle.DestinationOf(item.Shipment).WarehouseId, item.Shipment.DestinationCountryCode))
            .Select(item => (item.Shipment, item.Parcels)).ToList();

        if (here.ClearsCustoms && found.Count > 0)
        {
            IReadOnlyList<OperationsTrackingEvent> history = await _tracking.ListForSubjectsAsync(OperationsTrackingSubject.Parcel,
                found.SelectMany(item => item.Parcels).Select(parcel => parcel.Id).ToList(), cancellationToken);
            HashSet<Guid> voided = history.Where(item => item.VoidsEventId is not null).Select(item => item.VoidsEventId!.Value).ToHashSet();
            HashSet<Guid> cleared = history.Where(item => item.Code == OperationsTrackingCodes.CustomsCleared && !voided.Contains(item.Id)).Select(item => item.SubjectId).ToHashSet();
            found = found.Select(item => (item.Shipment, (IReadOnlyList<OperationsParcel>)item.Parcels.Where(parcel => cleared.Contains(parcel.Id)).ToList()))
                .Where(item => item.Item2.Count > 0).ToList();
        }

        List<Guid> ids = found.Select(item => item.Shipment.Id).ToList();
        ILookup<Guid, OperationsInvoice> invoices = (await _pickups.ListInvoicesAsync(ids, cancellationToken)).ToLookup(invoice => invoice.ShipmentId!.Value);
        IReadOnlySet<Guid> collected = await _routes.CollectedAsync(ids, cancellationToken);
        return found.Select(item =>
        {
            (decimal due, bool waiting, string? currency) = Owed(invoices[item.Shipment.Id], collected);
            return (item.Shipment, item.Parcels, due, waiting, currency);
        }).ToList();
    }

    // A route for its planner (ops.delivery.plan at its warehouse), or for its own driver (ops.delivery.drive there); none otherwise (not found).
    public async Task<(OperationsAccessView? Access, OperationsDeliveryRouteView? View, bool Planner, Error? Refused)> RouteForAsync(OperationsAuthorizationContext context, Guid routeId,
        CancellationToken cancellationToken)
    {
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> planned, Error? refused) = await OpenAsync(context, OperationsPermissions.DeliveryPlan, cancellationToken);

        if (access is null)
        {
            return (null, null, false, refused);
        }

        (_, IReadOnlyList<OperationsWarehouse> driven, Error? notDriver) = await OpenAsync(context, OperationsPermissions.DeliveryDrive, cancellationToken);

        if (refused is not null && notDriver is not null)
        {
            return (access, null, false, refused);
        }

        if (await RouteAsync(routeId, planned.Concat(driven).DistinctBy(item => item.Id).ToList(), cancellationToken) is not { } view)
        {
            return (access, null, false, OperationsDeliveryErrors.NotFound);
        }

        if (planned.Any(item => item.Id == view.Route.WarehouseId))
        {
            return (access, view, true, null);
        }

        return view.Route.DriverUserId == access.User.Id ? (access, view, false, null) : (access, null, false, OperationsDeliveryErrors.NotFound);
    }

    // The route (tracked) with its warehouse, stops, shipments and parcels; none when its warehouse is not one of these.
    public async Task<OperationsDeliveryRouteView?> RouteAsync(Guid routeId, IReadOnlyList<OperationsWarehouse> warehouses, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(warehouses);

        if (await _routes.GetAsync(routeId, cancellationToken) is not { } route || warehouses.FirstOrDefault(item => item.Id == route.WarehouseId) is not { } here)
        {
            return null;
        }

        IReadOnlyList<OperationsDeliveryStop> stops = await _routes.ListStopsAsync(route.Id, cancellationToken);
        IReadOnlyList<OperationsShipment> shipments = await _routes.ListShipmentsAsync(stops.Select(stop => stop.ShipmentId).Distinct().ToList(), cancellationToken);
        return new OperationsDeliveryRouteView(route, here, stops, shipments.ToDictionary(shipment => shipment.Id), await _routes.ListRouteParcelsAsync(route.Id, cancellationToken));
    }

    // The route's stops as a page shows them, with what is owed for each.
    public async Task<IReadOnlyList<OperationsDeliveryStopRow>> StopRowsAsync(OperationsDeliveryRouteView view, OperationsTenantSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(settings);
        ILookup<Guid, OperationsInvoice> invoices = (await _pickups.ListInvoicesAsync(view.Shipments.Keys.ToList(), cancellationToken)).ToLookup(invoice => invoice.ShipmentId!.Value);
        IReadOnlySet<Guid> collected = await _routes.CollectedAsync(view.Shipments.Keys.ToList(), cancellationToken);
        return view.Stops.Select(stop =>
        {
            OperationsShipment? shipment = view.Shipments.GetValueOrDefault(stop.ShipmentId);
            (decimal due, bool waiting, string? currency) = Owed(invoices[stop.ShipmentId], stop.IsDone ? null : collected);
            return new OperationsDeliveryStopRow(stop.Id, stop.Sequence, stop.ShipmentId, shipment?.Number ?? string.Empty, shipment?.RecipientName, shipment?.RecipientPhone,
                shipment is null ? string.Empty : AddressLine(shipment), stop.ParcelCount, due, currency, stop.Status)
            {
                ReceiverName = stop.ReceiverName, IdKind = stop.IdKind, Signed = stop.SignatureKey is not null, Photographed = stop.PhotoKey is not null, CodeChecked = stop.CodeChecked, FromSheet = stop.FromSheet,
                CashCollected = stop.CashCollected, FailureReason = stop.FailureReason, Note = stop.Note, DoneUtc = stop.DoneUtc,
                Blocked = !stop.IsDone && Blocked(due, waiting, settings, view.Warehouse)
            };
        }).ToList();
    }

    // What is owed at the door now: the unpaid invoices less the cash already collected on routes not settled (review: never collected twice).
    public async Task<(decimal Due, bool Waiting, IReadOnlyList<Guid> Invoices)> DueAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationsInvoice> invoices = await _pickups.ListInvoicesAsync(new[] { shipmentId }, cancellationToken);
        IReadOnlySet<Guid> collected = await _routes.CollectedAsync(new[] { shipmentId }, cancellationToken);
        (decimal due, bool waiting, _) = Owed(invoices, collected);
        return (due, waiting, invoices.Where(invoice => invoice.PaymentState == OperationsPaymentState.Unpaid && !collected.Contains(invoice.Id)).Select(invoice => invoice.Id).ToList());
    }

    // Still fit to go out: at the warehouse or out for delivery, not held by customs, not going back (review: held after planning).
    public static bool Deliverable(OperationsParcel parcel) =>
        (parcel ?? throw new ArgumentNullException(nameof(parcel))).Milestone is OperationsMilestone.ArrivedDestinationWarehouse or OperationsMilestone.OutForDelivery
        && (parcel.Flags & (OperationsParcelFlags.CustomsHeld | OperationsParcelFlags.ReturnPending)) == 0;

    public Task<IReadOnlyList<OperationsInvoice>> InvoicesAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        _pickups.ListInvoicesAsync(new[] { shipmentId }, cancellationToken);

    public async Task<OperationsWarehouse?> WarehouseAsync(Guid warehouseId, CancellationToken cancellationToken) => await _locations.GetWarehouseAsync(warehouseId, cancellationToken);
}
