using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Arrivals;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Arrivals.FinishOperationsArrival;

// =========================================================
// FILE: FinishOperationsArrivalUseCase.cs
//
// PURPOSE:
// Finish receiving a dispatch: what did not come is listed as missing (Slice 7, R-2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Refused while a bundle that ends here is unopened
// - A late scan still closes a missing line (R-2)
// =========================================================
public sealed class FinishOperationsArrivalUseCase
{
    private readonly OperationsArrivalDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly Vixly.Operations.Application.Tracking.OperationsTracker _tracker;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public FinishOperationsArrivalUseCase(
        OperationsArrivalDesk desk,
        IOperationsDispatchRepository dispatches,
        Vixly.Operations.Application.Tracking.OperationsTracker tracker,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<FinishOperationsArrivalResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        FinishOperationsArrivalRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.ArrivalsReceive, cancellationToken);

        if (refused is not null)
        {
            return FinishOperationsArrivalResponse.Failure(refused);
        }

        (OperationsDispatch? dispatch, OperationsWarehouse? here) = await _desk.DispatchAsync(request.DispatchId, request.WarehouseId, warehouses, cancellationToken);

        if (dispatch is null || here is null)
        {
            return FinishOperationsArrivalResponse.Failure(OperationsArrivalErrors.NotFound);
        }

        if (dispatch.ReceivedUtc is not null)
        {
            return FinishOperationsArrivalResponse.Failure(OperationsArrivalErrors.Received);
        }

        IReadOnlyList<OperationsBundle> bundles = await _dispatches.ListDispatchBundlesAsync(dispatch.Id, cancellationToken);
        Dictionary<Guid, OperationsDispatchBundle> links = (await _dispatches.ListLinksAsync(dispatch.Id, cancellationToken)).ToDictionary(item => item.BundleId);

        // Arrived on this dispatch, wherever it went next (review).
        bool Arrived(OperationsBundle bundle) => links[bundle.Id].ArrivedUtc is not null;

        // Each bundle that ends here, still here, is opened first, so its parcels were scanned (R-1).
        if (bundles.Any(bundle => Arrived(bundle) && OperationsArrivalDesk.EndsAt(here, bundle) && bundle.CurrentWarehouseId == here.Id && bundle.OpenedUtc is null))
        {
            return FinishOperationsArrivalResponse.Failure(OperationsArrivalErrors.BundlesUnopened);
        }

        IReadOnlyList<OperationsParcel> parcels = await _dispatches.ListDispatchParcelsAsync(dispatch.Id, false, cancellationToken);
        HashSet<Guid> scanned = await _desk.ScannedAsync(dispatch, parcels, cancellationToken);
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        List<(OperationsDiscrepancyKind Kind, Guid? BundleId, Guid? ParcelId, string Code)> lines = new();

        foreach (OperationsBundle bundle in bundles)
        {
            // A scan of the bundle at the same time changes the same link row: one of the two waits (review).
            if (!Arrived(bundle))
            {
                links[bundle.Id].Missing(utcNow);
                lines.Add((OperationsDiscrepancyKind.MissingBundle, bundle.Id, null, bundle.Number));
                continue;
            }

            if (!OperationsArrivalDesk.EndsAt(here, bundle))
            {
                continue;
            }

            lines.AddRange(parcels.Where(item => item.BundleId == bundle.Id && !scanned.Contains(item.Id) && !Vixly.Operations.Application.Events.OperationsEventDesk.Finished(item))
                .Select(parcel => (OperationsDiscrepancyKind.MissingParcel, (Guid?)bundle.Id, (Guid?)parcel.Id, parcel.Number ?? parcel.Id.ToString("N"))));
        }

        await _desk.NoteAsync(dispatch, here, lines, cancellationToken);
        int missing = lines.Count;

        try
        {
            dispatch.Received(utcNow);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return FinishOperationsArrivalResponse.Failure(exception.Error);
        }

        if ((await _tracker.RecordAsync(new OperationsTrackingDraft(OperationsTrackingSubject.Dispatch, dispatch.Id, OperationsTrackingCodes.DispatchReceived,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["warehouse"] = here.Name }, utcNow, "dispatch:" + dispatch.Id.ToString("N") + ":received",
            OperationsTrackingActor.Staff, OperationsTrackingSource.Manual) { ActorUserId = access!.User.Id }, cancellationToken)).Error is { } refusedEvent)
        {
            _unitOfWork.DiscardChanges();
            return FinishOperationsArrivalResponse.Failure(refusedEvent);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return FinishOperationsArrivalResponse.Success(missing);
        }

        _unitOfWork.DiscardChanges();
        return FinishOperationsArrivalResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
