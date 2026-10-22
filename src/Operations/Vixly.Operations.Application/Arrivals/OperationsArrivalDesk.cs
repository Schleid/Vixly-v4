using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Dispatch;
using Vixly.Operations.Application.Events;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Arrivals;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Arrivals;

// =========================================================
// FILE: OperationsArrivalDesk.cs
//
// PURPOSE:
// What every destination-receiving use case shares (Slice 7 step 1, R-1 to R-3, R-11).
//
// RESPONSIBILITIES:
// - Say which dispatches a warehouse receives, and whether a bundle or shipment ends there
// - Arrive a bundle (and the leg under way), arrive parcels, and make them ready for pickup
//
// IMPORTANT NOTES:
// - The caller saves; a refused event is returned and the caller abandons its save
// - Every event has a key from the dispatch and the bundle or parcel, so a repeat scan writes nothing
// - A dispatch is received where it was bound when planned (R-11); what arrived is kept on its link to each bundle
// =========================================================
public sealed class OperationsArrivalDesk
{
    private readonly OperationsBundleDesk _bundles;
    private readonly IOperationsArrivalRepository _arrivals;
    private readonly IOperationsDispatchRepository _dispatches;
    private readonly IOperationsBundleRepository _bundleStore;
    private readonly IOperationsLocationRepository _locations;
    private readonly IOperationsReceivingRepository _receiving;
    private readonly IOperationsTrackingRepository _tracking;
    private readonly OperationsTracker _tracker;
    private readonly OperationsDispatching _dispatching;
    private readonly OperationsOutboxWriter _outbox;
    private readonly TimeProvider _timeProvider;

    public OperationsArrivalDesk(
        OperationsBundleDesk bundles,
        IOperationsArrivalRepository arrivals,
        IOperationsDispatchRepository dispatches,
        IOperationsBundleRepository bundleStore,
        IOperationsLocationRepository locations,
        IOperationsReceivingRepository receiving,
        IOperationsTrackingRepository tracking,
        OperationsTracker tracker,
        OperationsDispatching dispatching,
        OperationsOutboxWriter outbox,
        TimeProvider timeProvider)
    {
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _arrivals = arrivals ?? throw new ArgumentNullException(nameof(arrivals));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _bundleStore = bundleStore ?? throw new ArgumentNullException(nameof(bundleStore));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _dispatching = dispatching ?? throw new ArgumentNullException(nameof(dispatching));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Task<(OperationsAccessView? Access, IReadOnlyList<OperationsWarehouse> Warehouses, Error? Refused)> OpenAsync(OperationsAuthorizationContext context, string permission,
        CancellationToken cancellationToken) =>
        _bundles.OpenAsync(context, new[] { permission }, cancellationToken);

    // Receiving or customs: the pages that show both.
    public Task<(OperationsAccessView? Access, IReadOnlyList<OperationsWarehouse> Warehouses, Error? Refused)> OpenEitherAsync(OperationsAuthorizationContext context,
        CancellationToken cancellationToken) =>
        _bundles.OpenAsync(context, new[] { OperationsPermissions.ArrivalsReceive, OperationsPermissions.CustomsRecord }, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, OperationsWarehouse>> WarehousesAsync(CancellationToken cancellationToken) =>
        (await _locations.ListWarehousesAsync(cancellationToken)).ToDictionary(item => item.Id);

    // The warehouse a dispatch is received at is this one (R-11, review): the one fixed when it was planned (its destination, or the hub serving it),
    // or, for a dispatch to a country only, the first warehouse there that scanned it.
    public static bool BoundFor(OperationsDispatch dispatch, OperationsWarehouse warehouse)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(warehouse);

        if (dispatch.WarehouseId == warehouse.Id || dispatch.Status is not (OperationsDispatchStatus.Departed or OperationsDispatchStatus.Arrived))
        {
            return false;
        }

        return dispatch.ReceivingWarehouseId is { } receiving
            ? receiving == warehouse.Id
            : dispatch.DestinationWarehouseId is null && string.Equals(warehouse.Address.CountryCode, dispatch.DestinationCountryCode, StringComparison.OrdinalIgnoreCase);
    }

    // Its journey ends here: the warehouse it is for, or, with none, a warehouse in its country.
    public static bool EndsAt(OperationsWarehouse warehouse, Guid? destinationWarehouseId, string? countryCode)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        return destinationWarehouseId is { } id ? id == warehouse.Id : string.Equals(warehouse.Address.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase);
    }

    public static bool EndsAt(OperationsWarehouse warehouse, OperationsBundle bundle) =>
        EndsAt(warehouse, (bundle ?? throw new ArgumentNullException(nameof(bundle))).DestinationWarehouseId, bundle.DestinationCountryCode);

    // A dispatch this person receives at this warehouse; none when it is not bound here.
    public async Task<(OperationsDispatch? Dispatch, OperationsWarehouse? Warehouse)> DispatchAsync(Guid dispatchId, Guid warehouseId, IReadOnlyList<OperationsWarehouse> mine,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mine);

        if (mine.FirstOrDefault(item => item.Id == warehouseId) is not { } warehouse
            || await _dispatches.GetDispatchAsync(dispatchId, cancellationToken) is not { } dispatch
            || !BoundFor(dispatch, warehouse))
        {
            return (null, null);
        }

        return (dispatch, warehouse);
    }

    // A bundle of the dispatch is here: the leg under way arrives with the first one; at a hub its parcels are here too, in transit.
    // A repeat changes nothing; a bundle that went on since (a later dispatch, another warehouse) is refused (review).
    public async Task<Error?> ArriveBundleAsync(OperationsDispatch dispatch, OperationsDispatchBundle link, OperationsBundle bundle, OperationsWarehouse here, Guid userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(link);
        ArgumentNullException.ThrowIfNull(bundle);
        ArgumentNullException.ThrowIfNull(here);

        if (link.ArrivedUtc is not null)
        {
            return null;
        }

        if ((bundle.DispatchId is { } on && on != dispatch.Id) || (bundle.DispatchId is null && bundle.CurrentWarehouseId is { } at && at != here.Id))
        {
            return OperationsArrivalErrors.MovedOn;
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        dispatch.ClaimReceiving(here.Id, utcNow);
        link.Arrive(utcNow);
        bundle.ArriveAt(here.Id, utcNow);

        if (dispatch.LegUnderWay() is { } leg)
        {
            DateTimeOffset when = leg.DepartedUtc is { } departed && departed > utcNow ? departed : utcNow;
            bool last = dispatch.ArriveLeg(leg.Id, when, utcNow);
            await _dispatching.ArrivedAsync(dispatch, leg, last, last ? await _dispatching.ParcelsAsync(dispatch, cancellationToken) : Array.Empty<OperationsParcel>(), userId,
                cancellationToken);
        }

        OperationsTrackingResult recorded = await _tracker.RecordAsync(new OperationsTrackingDraft(OperationsTrackingSubject.Bundle, bundle.Id, OperationsTrackingCodes.BundleArrived,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["warehouse"] = here.Name }, utcNow, Key(dispatch, "bundle", bundle.Id), OperationsTrackingActor.Staff,
            OperationsTrackingSource.Scan) { ActorUserId = userId }, cancellationToken);

        if (recorded.Error is { } refused)
        {
            return refused;
        }

        await FoundAsync(new[] { bundle.Id }, Array.Empty<Guid>(), cancellationToken);

        // At a hub the bundle goes on unopened: its parcels are here, in transit (R-11).
        return EndsAt(here, bundle)
            ? null
            : (await ArriveParcelsAsync(dispatch, await _bundleStore.ListBundleParcelsForUpdateAsync(bundle.Id, cancellationToken), here, false, userId, cancellationToken)).Refused;
    }

    // Opened at its destination: its parcels are scanned one by one (R-1).
    public async Task<Error?> OpenBundleAsync(OperationsDispatch dispatch, OperationsBundle bundle, OperationsWarehouse here, Guid userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(bundle);
        ArgumentNullException.ThrowIfNull(here);

        if (bundle.OpenedUtc is not null)
        {
            return null;
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        bundle.OpenAtDestination(utcNow);
        return (await _tracker.RecordAsync(new OperationsTrackingDraft(OperationsTrackingSubject.Bundle, bundle.Id, OperationsTrackingCodes.BundleOpened,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["warehouse"] = here.Name }, utcNow, Key(dispatch, "opened", bundle.Id), OperationsTrackingActor.Staff,
            OperationsTrackingSource.Scan) { ActorUserId = userId }, cancellationToken)).Error;
    }

    // Parcels are here: at their destination they arrived, at a hub they are in transit. A parcel already scanned on this dispatch is left as it is (review).
    public async Task<(Error? Refused, int Fresh)> ArriveParcelsAsync(OperationsDispatch dispatch, IReadOnlyList<OperationsParcel> parcels, OperationsWarehouse here, bool endsHere,
        Guid userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(parcels);
        ArgumentNullException.ThrowIfNull(here);
        HashSet<string> done = (await _tracking.FindByKeysAsync(parcels.Select(parcel => ParcelKey(dispatch, parcel, endsHere)).ToList(), cancellationToken))
            .Select(item => item.IdempotencyKey!).ToHashSet(StringComparer.Ordinal);
        List<OperationsParcel> going = parcels.Where(parcel => !OperationsEventDesk.Finished(parcel) && !done.Contains(ParcelKey(dispatch, parcel, endsHere))).ToList();
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        foreach (OperationsParcel parcel in going)
        {
            parcel.ArriveAt(here.Id, utcNow);
        }

        string code = endsHere ? OperationsTrackingCodes.ArrivedDestinationWarehouse : OperationsTrackingCodes.InTransit;

        if (await _tracker.RecordManyAsync(going.Select(parcel => (new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, code,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["place"] = here.Name }, utcNow, ParcelKey(dispatch, parcel, endsHere), OperationsTrackingActor.Staff,
            OperationsTrackingSource.Scan) { ActorUserId = userId }, parcel)).ToList(), cancellationToken) is { } refused)
        {
            return (refused, 0);
        }

        await FoundAsync(Array.Empty<Guid>(), going.Select(parcel => parcel.Id).ToList(), cancellationToken);
        return (null, going.Count);
    }

    // The parcels scanned in at their destination on this dispatch, in one query.
    public async Task<HashSet<Guid>> ScannedAsync(OperationsDispatch dispatch, IReadOnlyList<OperationsParcel> parcels, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcels);
        return (await _tracking.FindByKeysAsync(parcels.Select(parcel => ParcelKey(dispatch, parcel, true)).ToList(), cancellationToken)).Select(item => item.SubjectId).ToHashSet();
    }

    public static string ParcelKey(OperationsDispatch dispatch, OperationsParcel parcel, bool endsHere) =>
        Key(dispatch ?? throw new ArgumentNullException(nameof(dispatch)), endsHere ? "here" : "transit", (parcel ?? throw new ArgumentNullException(nameof(parcel))).Id);

    public static string Key(OperationsDispatch dispatch, string step, Guid id) =>
        "arr:" + (dispatch ?? throw new ArgumentNullException(nameof(dispatch))).Id.ToString("N") + ":" + step + ":" + id.ToString("N");

    // What was missing turned up: closed by itself (R-2).
    public async Task FoundAsync(IReadOnlyCollection<Guid> bundleIds, IReadOnlyCollection<Guid> parcelIds, CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        foreach (OperationsArrivalDiscrepancy open in await _arrivals.ListOpenForAsync(bundleIds, parcelIds, cancellationToken))
        {
            if (open.Kind is OperationsDiscrepancyKind.MissingBundle or OperationsDiscrepancyKind.MissingParcel)
            {
                open.Found(utcNow);
            }
        }
    }

    // Open it once per item and dispatch (R-2); the open ones loaded in one query (review).
    public async Task<int> NoteAsync(OperationsDispatch? dispatch, OperationsWarehouse here,
        IReadOnlyList<(OperationsDiscrepancyKind Kind, Guid? BundleId, Guid? ParcelId, string Code)> items, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(here);
        ArgumentNullException.ThrowIfNull(items);
        IReadOnlyList<OperationsArrivalDiscrepancy> open = await _arrivals.ListOpenForAsync(
            items.Where(item => item.ParcelId is null && item.BundleId is not null).Select(item => item.BundleId!.Value).Distinct().ToList(),
            items.Where(item => item.ParcelId is not null).Select(item => item.ParcelId!.Value).Distinct().ToList(), cancellationToken);
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        int noted = 0;

        foreach ((OperationsDiscrepancyKind kind, Guid? bundleId, Guid? parcelId, string code) in items)
        {
            if (open.Any(item => item.DispatchId == dispatch?.Id && item.ParcelId == parcelId && (parcelId is not null || item.BundleId == bundleId)))
            {
                continue;
            }

            OperationsArrivalDiscrepancy added = OperationsArrivalDiscrepancy.Create(here.TenantId, dispatch?.Id, here.Id, kind, bundleId, parcelId, code, utcNow);
            _arrivals.Add(added);
            open = open.Append(added).ToList();
            noted++;
        }

        return noted;
    }

    // Ready for pickup (R-1): at the warehouse its shipment goes to, for pickup, not held by customs, and cleared when this warehouse clears customs.
    // The customer is told once every parcel of the shipment still going is ready, when the company sends that email.
    public async Task<(Error? Refused, int Ready)> ReadyAsync(IReadOnlyList<OperationsParcel> parcels, OperationsWarehouse here, Guid userId, string? portalSlug,
        OperationsTenantSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcels);
        ArgumentNullException.ThrowIfNull(here);
        ArgumentNullException.ThrowIfNull(settings);
        List<OperationsParcel> candidates = parcels.Where(parcel => parcel.WarehouseId == here.Id && parcel.ShipmentId is not null && !OperationsEventDesk.Finished(parcel)
            && !parcel.Flags.HasFlag(OperationsParcelFlags.CustomsHeld) && (parcel.Milestone is null || parcel.Milestone < OperationsMilestone.ReadyForPickup)).ToList();

        if (candidates.Count == 0)
        {
            return (null, 0);
        }

        Dictionary<Guid, OperationsShipment> shipments = (await _dispatches.ListShipmentsAsync(candidates.Select(parcel => parcel.ShipmentId!.Value).ToList(), cancellationToken))
            .ToDictionary(item => item.Id);
        HashSet<Guid> cleared = new();

        if (here.ClearsCustoms)
        {
            IReadOnlyList<OperationsTrackingEvent> history = await _tracking.ListForSubjectsAsync(OperationsTrackingSubject.Parcel, candidates.Select(parcel => parcel.Id).ToList(),
                cancellationToken);
            HashSet<Guid> voided = history.Where(item => item.VoidsEventId is not null).Select(item => item.VoidsEventId!.Value).ToHashSet();
            cleared = history.Where(item => item.Code == OperationsTrackingCodes.CustomsCleared && !voided.Contains(item.Id)).Select(item => item.SubjectId).ToHashSet();
        }

        List<OperationsParcel> ready = candidates.Where(parcel => shipments.TryGetValue(parcel.ShipmentId!.Value, out OperationsShipment? shipment) && shipment.DeliveryAddress is null
            && EndsAt(here, OperationsBundle.DestinationOf(shipment).WarehouseId, shipment.DestinationCountryCode) && (!here.ClearsCustoms || cleared.Contains(parcel.Id))).ToList();
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        // Slice 7 step 2 (R-10): storage runs from now, with the company's destination terms of today (no fee when storage is off).
        foreach (OperationsParcel parcel in ready)
        {
            parcel.MarkReady(settings.DestinationStorageFreeDays, settings.DestinationStorage ? settings.DestinationStorageFeePerDay : null, utcNow);
        }

        if (await _tracker.RecordManyAsync(ready.Select(parcel => (new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.ReadyForPickup,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["place"] = here.Name }, utcNow, "ready:" + here.Id.ToString("N") + ":" + parcel.Id.ToString("N"),
            OperationsTrackingActor.Staff, OperationsTrackingSource.Scan) { ActorUserId = userId }, parcel)).ToList(), cancellationToken) is { } refused)
        {
            return (refused, 0);
        }

        // Slice 7 step 2 (R-10, review): the shipment keeps when it first had a parcel ready, for its reminders.
        foreach (Guid shipmentId in ready.Select(parcel => parcel.ShipmentId!.Value).Distinct())
        {
            (await _receiving.GetShipmentAsync(shipmentId, cancellationToken))?.MarkPickupReady(utcNow);
        }

        // The handler sends it once every parcel of the shipment still going is ready, and once per shipment (review: two last parcels scanned at once).
        if (settings.PickupEmails && !string.IsNullOrWhiteSpace(portalSlug))
        {
            foreach (OperationsShipment shipment in ready.Select(parcel => shipments[parcel.ShipmentId!.Value]).DistinctBy(item => item.Id))
            {
                _outbox.Write(OperationsPickupNotice.MessageType, here.TenantId, shipment.Id, new OperationsPickupNotice(shipment.CustomerId, portalSlug, shipment.Number ?? string.Empty,
                    here.Name, Line(here.Address), here.OpeningHours));
            }
        }

        return (null, ready.Count);
    }

    private static string Line(OperationsPostalAddress address) =>
        string.Join(", ", new[] { address.Line1, address.Line2, address.City, address.PostalCode }.Where(part => !string.IsNullOrWhiteSpace(part)));
}
