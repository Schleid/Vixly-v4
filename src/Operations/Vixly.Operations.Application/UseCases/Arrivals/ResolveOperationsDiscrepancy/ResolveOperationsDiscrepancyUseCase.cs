using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Events;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Arrivals;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Arrivals.ResolveOperationsDiscrepancy;

// =========================================================
// FILE: ResolveOperationsDiscrepancyUseCase.cs
//
// PURPOSE:
// Resolve a discrepancy: lost (the customer is told) or settled, with a note (Slice 7, R-2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.arrivals.receive at the destination or the origin (R-2)
// - Lost writes the missing_in_transit exception; own company causes are not offered here
// =========================================================
public sealed class ResolveOperationsDiscrepancyUseCase
{
    private readonly OperationsArrivalDesk _desk;

    private readonly IOperationsArrivalRepository _arrivals;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsTracker _tracker;

    private readonly OperationsOutboxWriter _outbox;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ResolveOperationsDiscrepancyUseCase(
        OperationsArrivalDesk desk,
        IOperationsArrivalRepository arrivals,
        IOperationsDispatchRepository dispatches,
        IOperationsBundleRepository bundles,
        IOperationsTrackingRepository tracking,
        IOperationsReceivingRepository receiving,
        OperationsTracker tracker,
        OperationsOutboxWriter outbox,
        IOperationsSettingsRepository settings,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _arrivals = arrivals ?? throw new ArgumentNullException(nameof(arrivals));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ResolveOperationsDiscrepancyResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ResolveOperationsDiscrepancyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.ArrivalsReceive, cancellationToken);

        if (refused is not null)
        {
            return ResolveOperationsDiscrepancyResponse.Failure(refused);
        }

        if (await _arrivals.GetDiscrepancyAsync(request.DiscrepancyId, cancellationToken) is not { } item)
        {
            return ResolveOperationsDiscrepancyResponse.Failure(OperationsArrivalErrors.NotFound);
        }

        OperationsDispatch? dispatch = item.DispatchId is { } dispatchId ? await _dispatches.GetDispatchAsync(dispatchId, cancellationToken) : null;

        if (!warehouses.Any(warehouse => warehouse.Id == item.WarehouseId || warehouse.Id == dispatch?.WarehouseId))
        {
            return ResolveOperationsDiscrepancyResponse.Failure(OperationsArrivalErrors.NotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            item.Resolve(request.Outcome, request.Note, access!.User.Id, utcNow);
        }
        catch (DomainException exception)
        {
            return ResolveOperationsDiscrepancyResponse.Failure(exception.Error);
        }

        // Lost in transit: an exception on each parcel still going, and the customer is told when the company sends those emails (R-2).
        if (item.Status == OperationsDiscrepancyStatus.Lost && item.Kind is OperationsDiscrepancyKind.MissingBundle or OperationsDiscrepancyKind.MissingParcel)
        {
            List<OperationsParcel> lost = item.Kind == OperationsDiscrepancyKind.MissingParcel
                ? (item.ParcelId is { } parcelId && await _tracking.GetParcelAsync(parcelId, cancellationToken) is { } one ? new List<OperationsParcel> { one } : new List<OperationsParcel>())
                : (await _bundles.ListBundleParcelsForUpdateAsync(item.BundleId!.Value, cancellationToken)).Where(parcel => parcel.WarehouseId != item.WarehouseId).ToList();
            lost = lost.Where(parcel => !OperationsEventDesk.Finished(parcel)).ToList();
            Dictionary<string, string> arguments = new(StringComparer.Ordinal) { ["cause"] = "missing_in_transit" };

            if (dispatch is not null)
            {
                arguments["source"] = dispatch.Number;
            }

            if (await _tracker.RecordManyAsync(lost.Select(parcel => (new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.ExceptionRaised,
                arguments, utcNow, "lost:" + item.Id.ToString("N") + ":" + parcel.Id.ToString("N"), OperationsTrackingActor.Staff, OperationsTrackingSource.Manual)
                { ActorUserId = access!.User.Id, Note = item.Note }, parcel)).ToList(), cancellationToken) is { } failed)
            {
                _unitOfWork.DiscardChanges();
                return ResolveOperationsDiscrepancyResponse.Failure(failed);
            }

            Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
                ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            if (settings.EventEmails && !string.IsNullOrWhiteSpace(request.PortalSlug))
            {
                foreach (Guid shipmentId in lost.Where(parcel => parcel.ShipmentId is not null).Select(parcel => parcel.ShipmentId!.Value).Distinct())
                {
                    if (await _receiving.GetShipmentAsync(shipmentId, cancellationToken) is { } shipment)
                    {
                        _outbox.Write(OperationsEventNotice.MessageType, context.RequiredTenantId, shipment.Id, new OperationsEventNotice(shipment.CustomerId, request.PortalSlug,
                            shipment.Number ?? string.Empty, true, "missing_in_transit", null, null, null, null));
                    }
                }
            }
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ResolveOperationsDiscrepancyResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return ResolveOperationsDiscrepancyResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
