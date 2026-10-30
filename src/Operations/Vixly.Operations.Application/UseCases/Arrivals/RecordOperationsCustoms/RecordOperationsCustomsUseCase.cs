using Vixly.Operations.Application.Arrivals;
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
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Events;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Arrivals.RecordOperationsCustoms;

// =========================================================
// FILE: RecordOperationsCustomsUseCase.cs
//
// PURPOSE:
// Record customs for a dispatch or one of its bundles at a warehouse that clears customs (Slice 7, R-3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.customs.record at a warehouse that clears customs (R-3)
// - Held parcels may not leave or be collected until cleared
// - One request key: a double click writes nothing more
// =========================================================
public sealed class RecordOperationsCustomsUseCase
{
    private readonly OperationsArrivalDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsBundleRepository _bundles;

    private readonly OperationsDispatching _dispatching;

    private readonly OperationsTracker _tracker;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RecordOperationsCustomsUseCase(
        OperationsArrivalDesk desk,
        IOperationsDispatchRepository dispatches,
        IOperationsBundleRepository bundles,
        OperationsDispatching dispatching,
        OperationsTracker tracker,
        IOperationsSettingsRepository settings,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _dispatching = dispatching ?? throw new ArgumentNullException(nameof(dispatching));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RecordOperationsCustomsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RecordOperationsCustomsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.CustomsRecord, cancellationToken);

        if (refused is not null)
        {
            return RecordOperationsCustomsResponse.Failure(refused);
        }

        (OperationsDispatch? dispatch, OperationsWarehouse? here) = await _desk.DispatchAsync(request.DispatchId, request.WarehouseId, warehouses, cancellationToken);

        if (dispatch is null || here is null)
        {
            return RecordOperationsCustomsResponse.Failure(OperationsArrivalErrors.NotFound);
        }

        // Customs is recorded where the company clears it (R-3).
        if (!here.ClearsCustoms)
        {
            return RecordOperationsCustomsResponse.Failure(OperationsArrivalErrors.CustomsNotHere);
        }

        string? code = request.Step switch
        {
            "in_customs" => OperationsTrackingCodes.InCustoms,
            "cleared" => OperationsTrackingCodes.CustomsCleared,
            "held" => OperationsTrackingCodes.CustomsHeld,
            _ => null
        };

        if (code is null)
        {
            return RecordOperationsCustomsResponse.Failure(OperationsArrivalErrors.CustomsStepInvalid);
        }

        string? message = string.IsNullOrWhiteSpace(request.Message) || code != OperationsTrackingCodes.CustomsHeld ? null : request.Message.Trim();

        if (message is not null && (message.Length > 140 || message.Any(char.IsControl) || message.IndexOfAny(['<', '>']) >= 0))
        {
            return RecordOperationsCustomsResponse.Failure(OperationsEventErrors.TextInvalid);
        }

        IReadOnlyList<OperationsBundle> carried = await _dispatches.ListDispatchBundlesAsync(dispatch.Id, cancellationToken);

        if (request.BundleId is { } bundleId && carried.All(item => item.Id != bundleId))
        {
            return RecordOperationsCustomsResponse.Failure(OperationsArrivalErrors.NotFound);
        }

        // Only what is here (review): the bundles scanned in on this dispatch and still here, and the parcels here; never parcels gone on or not arrived.
        HashSet<Guid> arrived = (await _dispatches.ListLinksAsync(dispatch.Id, cancellationToken)).Where(link => link.ArrivedUtc is not null).Select(link => link.BundleId).ToHashSet();
        Dictionary<Guid, OperationsBundle> hereBundles = carried.Where(bundle => arrived.Contains(bundle.Id) && bundle.CurrentWarehouseId == here.Id
            && (request.BundleId is null || bundle.Id == request.BundleId)).ToDictionary(bundle => bundle.Id);

        // A bundle still closed: all its parcels; once opened, only the parcels scanned in here (review).
        List<OperationsParcel> parcels = (await _dispatching.ParcelsAsync(dispatch, cancellationToken)).Where(parcel => parcel.BundleId is { } inBundle
            && hereBundles.TryGetValue(inBundle, out OperationsBundle? bundle) && (bundle.OpenedUtc is null || parcel.WarehouseId == here.Id)).ToList();

        if (parcels.Count == 0)
        {
            return RecordOperationsCustomsResponse.Failure(OperationsArrivalErrors.NotHere);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        List<OperationsParcel> going = parcels.Where(parcel => !OperationsEventDesk.Finished(parcel)).ToList();
        Dictionary<string, string> arguments = new(StringComparer.Ordinal);

        if (message is not null)
        {
            arguments["message"] = message;
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        string key = "cus:" + (request.RequestKey ?? Guid.NewGuid()).ToString("N") + ":";

        if (await _tracker.RecordManyAsync(going.Select(parcel => (new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, code, arguments, utcNow,
            key + parcel.Id.ToString("N"), OperationsTrackingActor.Staff, OperationsTrackingSource.Manual) { ActorUserId = access!.User.Id }, parcel)).ToList(), cancellationToken) is { } failed)
        {
            _unitOfWork.DiscardChanges();
            return RecordOperationsCustomsResponse.Failure(failed);
        }

        int ready = 0;

        // Cleared: the parcels already scanned in here may now be ready for pickup (R-1).
        if (code == OperationsTrackingCodes.CustomsCleared)
        {
            (Error? refusedReady, ready) = await _desk.ReadyAsync(going, here, access!.User.Id, request.PortalSlug, settings, cancellationToken);

            if (refusedReady is not null)
            {
                _unitOfWork.DiscardChanges();
                return RecordOperationsCustomsResponse.Failure(refusedReady);
            }
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return RecordOperationsCustomsResponse.Success(going.Count, ready);
        }

        _unitOfWork.DiscardChanges();
        return RecordOperationsCustomsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
