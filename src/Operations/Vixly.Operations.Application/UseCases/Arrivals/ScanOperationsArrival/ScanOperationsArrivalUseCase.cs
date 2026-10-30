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

namespace Vixly.Operations.Application.UseCases.Arrivals.ScanOperationsArrival;

// =========================================================
// FILE: ScanOperationsArrivalUseCase.cs
//
// PURPOSE:
// Scan a bundle, a repack box or a parcel arriving at a destination warehouse (Slice 7, R-1, R-2, R-11).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.arrivals.receive at the warehouse the dispatch is bound for (R-11)
// - A repeat scan writes nothing and says so
// - Something not on the dispatch is noted as extra, never received (R-2)
// =========================================================
public sealed class ScanOperationsArrivalUseCase
{
    private readonly OperationsArrivalDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsArrivalRepository _arrivals;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ScanOperationsArrivalUseCase(
        OperationsArrivalDesk desk,
        IOperationsDispatchRepository dispatches,
        IOperationsArrivalRepository arrivals,
        IOperationsBundleRepository bundles,
        IOperationsTrackingRepository tracking,
        IOperationsSettingsRepository settings,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _arrivals = arrivals ?? throw new ArgumentNullException(nameof(arrivals));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ScanOperationsArrivalResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ScanOperationsArrivalRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.ArrivalsReceive, cancellationToken);

        if (refused is not null)
        {
            return ScanOperationsArrivalResponse.Failure(refused);
        }

        (OperationsDispatch? dispatch, OperationsWarehouse? here) = await _desk.DispatchAsync(request.DispatchId, request.WarehouseId, warehouses, cancellationToken);

        if (dispatch is null || here is null)
        {
            return ScanOperationsArrivalResponse.Failure(OperationsArrivalErrors.NotFound);
        }

        string code = request.Code?.Trim() ?? string.Empty;

        if (code.Length is 0 or > OperationsArrivalDiscrepancy.CodeMaxLength || code.Any(char.IsControl))
        {
            return ScanOperationsArrivalResponse.Failure(OperationsArrivalErrors.UnknownCode);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        Guid userId = access!.User.Id;
        IReadOnlyList<OperationsBundle> carried = await _dispatches.ListDispatchBundlesAsync(dispatch.Id, cancellationToken);
        Dictionary<Guid, OperationsDispatchBundle> links = (await _dispatches.ListLinksAsync(dispatch.Id, cancellationToken)).ToDictionary(item => item.BundleId);
        string outcome;
        int ready = 0;
        Error? failed;

        try
        {
            if (carried.FirstOrDefault(item => string.Equals(item.Number, code, StringComparison.OrdinalIgnoreCase)) is { } bundle)
            {
                bool again = links[bundle.Id].ArrivedUtc is not null;
                failed = await _desk.ArriveBundleAsync(dispatch, links[bundle.Id], bundle, here, userId, cancellationToken);
                (outcome, code) = (again ? "bundle_again" : "bundle", bundle.Number);
            }
            else if (await _dispatches.FindBundleForUpdateAsync(code, cancellationToken) is { } stray)
            {
                // Not on this dispatch: noted for the staff to sort out (R-2).
                await _desk.NoteAsync(dispatch, here, new[] { (OperationsDiscrepancyKind.ExtraBundle, (Guid?)stray.Id, (Guid?)null, stray.Number) }, cancellationToken);
                (outcome, code, failed) = ("extra_bundle", stray.Number, null);
            }
            else
            {
                OperationsRepackBox? box = await _bundles.FindBoxByNumberAsync(code, cancellationToken);
                List<OperationsParcel> items = box is not null
                    ? (await _arrivals.ListBoxParcelsForUpdateAsync(box.Id, cancellationToken)).ToList()
                    : await _arrivals.FindParcelForUpdateAsync(code, cancellationToken) is { } found ? new List<OperationsParcel> { found } : new List<OperationsParcel>();

                if (items.Count == 0)
                {
                    return ScanOperationsArrivalResponse.Failure(OperationsArrivalErrors.UnknownCode);
                }

                code = box?.Number ?? items[0].Number ?? code;

                if (carried.FirstOrDefault(item => item.Id == items[0].BundleId) is not { } inside)
                {
                    await _desk.NoteAsync(dispatch, here, items.Select(extra => (OperationsDiscrepancyKind.ExtraParcel, extra.BundleId, (Guid?)extra.Id, extra.Number ?? code)).ToList(),
                        cancellationToken);

                    (outcome, failed) = ("extra_parcel", null);
                }
                else
                {
                    // The bundle arrives with its first parcel scanned, and is opened at its destination.
                    failed = await _desk.ArriveBundleAsync(dispatch, links[inside.Id], inside, here, userId, cancellationToken);
                    bool endsHere = OperationsArrivalDesk.EndsAt(here, inside);
                    bool again = (await _tracking.FindByKeysAsync(items.Select(item => OperationsArrivalDesk.ParcelKey(dispatch, item, endsHere)).ToList(), cancellationToken)).Count
                        >= items.Count;

                    // Gone on from here since (review): left as it is.
                    if (failed is null && inside.CurrentWarehouseId != here.Id)
                    {
                        failed = OperationsArrivalErrors.MovedOn;
                    }

                    if (failed is null && endsHere)
                    {
                        failed = await _desk.OpenBundleAsync(dispatch, inside, here, userId, cancellationToken)
                            ?? (await _desk.ArriveParcelsAsync(dispatch, items, here, true, userId, cancellationToken)).Refused;
                    }

                    if (failed is null && endsHere)
                    {
                        (failed, ready) = await _desk.ReadyAsync(items, here, userId, request.PortalSlug, settings, cancellationToken);
                    }

                    outcome = again ? "parcel_again" : "parcel";
                }
            }
        }
        catch (DomainException exception)
        {
            failed = exception.Error;
            outcome = string.Empty;
        }

        if (failed is not null)
        {
            _unitOfWork.DiscardChanges();
            return ScanOperationsArrivalResponse.Failure(failed);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ScanOperationsArrivalResponse.Success(outcome, code, ready);
        }

        _unitOfWork.DiscardChanges();
        return ScanOperationsArrivalResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
