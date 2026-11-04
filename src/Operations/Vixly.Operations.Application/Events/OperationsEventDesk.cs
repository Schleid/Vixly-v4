using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Events;

// =========================================================
// FILE: OperationsEventDesk.cs
//
// PURPOSE:
// Opens the events workspace for a person and resolves what an event is about (Slice 6C, E-6).
//
// RESPONSIBILITIES:
// - The warehouses where the person sees or adds events
// - A subject by kind and id, with the warehouses it leaves from and goes to
// - Its parcels, for update when an event reaches them (E-4)
// - The causes the company offers (E-3)
//
// IMPORTANT NOTES:
// - A subject is seen by staff of the warehouse it leaves from or goes to; another's is not found
// =========================================================
public sealed class OperationsEventDesk
{
    private readonly OperationsBundleDesk _desk;
    private readonly IOperationsTrackingRepository _tracking;
    private readonly IOperationsReceivingRepository _receiving;
    private readonly IOperationsBundleRepository _bundles;
    private readonly IOperationsDispatchRepository _dispatches;
    private readonly IOperationsEventRepository _events;

    public OperationsEventDesk(OperationsBundleDesk desk, IOperationsTrackingRepository tracking, IOperationsReceivingRepository receiving, IOperationsBundleRepository bundles,
        IOperationsDispatchRepository dispatches, IOperationsEventRepository events)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Task<(OperationsAccessView? Access, IReadOnlyList<OperationsWarehouse> Warehouses, Error? Refused)> OpenAsync(OperationsAuthorizationContext context, string permission,
        CancellationToken cancellationToken) =>
        _desk.OpenAsync(context, permission == OperationsPermissions.EventsView ? new[] { OperationsPermissions.EventsView, OperationsPermissions.EventsAdd } : new[] { permission }, cancellationToken);

    public async Task<OperationsEventSubject?> SubjectAsync(OperationsTrackingSubject kind, Guid id, CancellationToken cancellationToken)
    {
        switch (kind)
        {
            case OperationsTrackingSubject.Parcel when await _tracking.GetParcelAsync(id, cancellationToken) is { } parcel:
                OperationsShipment? shipment = parcel.ShipmentId is { } shipmentId ? await _receiving.GetShipmentAsync(shipmentId, cancellationToken) : null;
                return new OperationsEventSubject(kind, parcel.Id, parcel.Number ?? parcel.CarrierTrackingNumber ?? string.Empty, shipment?.OriginWarehouseId ?? parcel.WarehouseId,
                    shipment?.DestinationWarehouseId, parcel.Milestone, null) { HereWarehouseId = parcel.WarehouseId };
            case OperationsTrackingSubject.Shipment when await _receiving.GetShipmentAsync(id, cancellationToken) is { } owned:
                return new OperationsEventSubject(kind, owned.Id, owned.Number ?? string.Empty, owned.OriginWarehouseId, owned.DestinationWarehouseId, null, owned.Status.ToString());
            case OperationsTrackingSubject.Bundle when await _bundles.GetBundleAsync(id, cancellationToken) is { } bundle:
                return new OperationsEventSubject(kind, bundle.Id, bundle.Number, bundle.WarehouseId, bundle.DestinationWarehouseId, null, bundle.Status.ToString())
                    { HereWarehouseId = bundle.CurrentWarehouseId };
            case OperationsTrackingSubject.Dispatch when await _dispatches.GetDispatchAsync(id, cancellationToken) is { } dispatch:
                return new OperationsEventSubject(kind, dispatch.Id, dispatch.Number, dispatch.WarehouseId, dispatch.DestinationWarehouseId, null, dispatch.Status.ToString());
            default:
                return null;
        }
    }

    // Delivered, cancelled, returned or disposed of: no delay or exception reaches it any more (review).
    public static bool Finished(OperationsParcel parcel) =>
        (parcel ?? throw new ArgumentNullException(nameof(parcel))).Milestone is { } reached && (reached >= OperationsMilestone.Delivered || OperationsTrackingProjection.IsTerminal(reached));

    // The parcels an event on the subject reaches (E-4); for update.
    public async Task<IReadOnlyList<OperationsParcel>> ParcelsAsync(OperationsEventSubject subject, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subject);
        return subject.Kind switch
        {
            OperationsTrackingSubject.Parcel => await _tracking.GetParcelAsync(subject.Id, cancellationToken) is { } parcel ? new[] { parcel } : Array.Empty<OperationsParcel>(),
            OperationsTrackingSubject.Shipment => await _events.ListShipmentParcelsForUpdateAsync(subject.Id, cancellationToken),
            OperationsTrackingSubject.Bundle => await _bundles.ListBundleParcelsForUpdateAsync(subject.Id, cancellationToken),
            OperationsTrackingSubject.Dispatch => await _dispatches.ListDispatchParcelsAsync(subject.Id, true, cancellationToken),
            _ => Array.Empty<OperationsParcel>()
        };
    }

    // The causes offered for a kind: the standard ones not switched off, then the company's own that are active.
    public async Task<IReadOnlyList<OperationsEventCause>> CausesAsync(Vixly.Operations.Domain.Events.OperationsEventKind kind, Vixly.Operations.Domain.Settings.OperationsTenantSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        IReadOnlyList<string> disabled = settings.DisabledEventCauseList();
        return Vixly.Operations.Domain.Events.OperationsEventReasons.For(kind).Where(code => !disabled.Contains(code, StringComparer.Ordinal))
            .Select(code => new OperationsEventCause(code, null, null, null))
            .Concat((await _events.ListReasonsAsync(cancellationToken)).Where(reason => reason.Kind == kind && reason.IsActive)
                .OrderBy(reason => reason.TextEn, StringComparer.CurrentCultureIgnoreCase).Select(reason => new OperationsEventCause(reason.Code, reason.TextEn, reason.TextFr, reason.TextEs)))
            .ToList();
    }
}
