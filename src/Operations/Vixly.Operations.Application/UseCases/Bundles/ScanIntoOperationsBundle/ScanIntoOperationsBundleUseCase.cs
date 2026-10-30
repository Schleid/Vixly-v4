using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.ScanIntoOperationsBundle;

// =========================================================
// FILE: ScanIntoOperationsBundleUseCase.cs
//
// PURPOSE:
// A worker scans a parcel or a repack box into an open bundle (Slice 6A, D-3, D-4 and D-5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.fill, at the bundle's warehouse
// - Each refusal says why; a split across bundles is a warning, never a refusal (owner, 2026-10-20)
// - The packed event is written in the same save (D-9)
// =========================================================
public sealed class ScanIntoOperationsBundleUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsBundling _bundling;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ScanIntoOperationsBundleUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        IOperationsReceivingRepository receiving,
        IOperationsTrackingRepository tracking,
        OperationsBundling bundling,
        IOperationsSettingsRepository settings,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _bundling = bundling ?? throw new ArgumentNullException(nameof(bundling));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ScanIntoOperationsBundleResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ScanIntoOperationsBundleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesFill }, cancellationToken);

        if (refused is not null)
        {
            return ScanIntoOperationsBundleResponse.Failure(refused);
        }

        if (await _bundles.GetBundleAsync(request.BundleId, cancellationToken) is not { } bundle || warehouses.All(item => item.Id != bundle.WarehouseId))
        {
            return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.NotFound);
        }

        if (!bundle.IsOpen)
        {
            return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.NotOpen);
        }

        string code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();

        if (code.Length is 0 or > 40)
        {
            return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.UnknownCode);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        string scan = (request.ScanKey == Guid.Empty ? Guid.NewGuid() : request.ScanKey).ToString("N");
        ScanIntoOperationsBundleResponse result;

        OperationsBundleItem? listed;

        if (await _bundles.FindBoxByNumberAsync(code, cancellationToken) is { } box)
        {
            (Error? offList, listed) = await ListedAsync(bundle, null, box.Id, box.BundleId == bundle.Id, cancellationToken);
            result = offList is not null ? ScanIntoOperationsBundleResponse.Failure(offList) : await BoxAsync(bundle, box, settings, access!.User.Id, scan, cancellationToken);
        }
        else if (await _receiving.FindParcelByNumberAsync(code, cancellationToken) is { } found && await _tracking.GetParcelAsync(found.Id, cancellationToken) is { } parcel)
        {
            (Error? offList, listed) = await ListedAsync(bundle, parcel.Id, null, parcel.BundleId == bundle.Id, cancellationToken);
            result = offList is not null ? ScanIntoOperationsBundleResponse.Failure(offList) : await ParcelAsync(bundle, parcel, settings, access!.User.Id, scan, cancellationToken);
        }
        else
        {
            return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.UnknownCode);
        }

        // Scanned again while its line still shows missing (review): confirmed now.
        if (result.Succeeded && result.Repeat && listed is { IsScanned: false } again)
        {
            again.Scanned(access!.User.Id, _timeProvider.GetUtcNow());
            return await _unitOfWork.SaveChangesAsync(cancellationToken) ? result : ScanIntoOperationsBundleResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        if (!result.Succeeded || result.Repeat)
        {
            return result;
        }

        // On the list: confirmed by this scan (owner, 2026-10-21).
        listed?.Scanned(access!.User.Id, _timeProvider.GetUtcNow());

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return result;
        }

        // Another scan at the same moment changed the bundle or the parcel: scanned again, it is either in already (a repeat) or checked afresh.
        _unitOfWork.DiscardChanges();
        return ScanIntoOperationsBundleResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    // The bundle's list (owner, 2026-10-21): a bundle with a list takes only what is on it; what is on another open bundle's list goes there.
    private async Task<(Error? Refused, OperationsBundleItem? Listed)> ListedAsync(OperationsBundle bundle, Guid? parcelId, Guid? boxId, bool repeat, CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationsBundleItem> open = await _bundles.ListOpenItemsAsync(parcelId is { } parcel ? new[] { parcel } : Array.Empty<Guid>(),
            boxId is { } box ? new[] { box } : Array.Empty<Guid>(), cancellationToken);
        OperationsBundleItem? listed = open.FirstOrDefault(item => item.BundleId == bundle.Id);

        if (repeat)
        {
            return (null, listed);
        }

        if (listed is null && open.Any(item => item.BundleId != bundle.Id))
        {
            return (OperationsBundleErrors.OnAnotherList, null);
        }

        if (listed is null && await _bundles.HasListAsync(bundle.Id, cancellationToken))
        {
            return (OperationsBundleErrors.NotOnList, null);
        }

        return (null, listed);
    }

    private async Task<ScanIntoOperationsBundleResponse> ParcelAsync(OperationsBundle bundle, OperationsParcel parcel, Vixly.Operations.Domain.Settings.OperationsTenantSettings settings, Guid userId,
        string scan, CancellationToken cancellationToken)
    {
        if (parcel.BundleId == bundle.Id)
        {
            return ScanIntoOperationsBundleResponse.Success(parcel.Id, parcel.BoxId, true, false, false);
        }

        if (parcel.WarehouseId != bundle.WarehouseId)
        {
            return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.WrongWarehouse);
        }

        if (parcel.BundleId is not null)
        {
            return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.InAnotherBundle);
        }

        OperationsShipment? shipment = parcel.ShipmentId is { } shipmentId ? await _receiving.GetShipmentAsync(shipmentId, cancellationToken) : null;

        if (OperationsBundling.Ready(parcel, shipment) is { } notReady)
        {
            return ScanIntoOperationsBundleResponse.Failure(notReady);
        }

        if (bundle.Refuses(shipment!) is { } wrong)
        {
            return ScanIntoOperationsBundleResponse.Failure(wrong);
        }

        if (!await _bundling.PaidAsync(shipment!, cancellationToken))
        {
            return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.NotPaid);
        }

        // A parcel repacked in a box goes in with its box (D-4).
        if (parcel.BoxId is not null)
        {
            return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.InAnotherBox);
        }

        List<OperationsParcel> others = OperationsShipmentBooking.Counted(await _receiving.ListShipmentParcelsAsync(shipment!.Id, cancellationToken)).Where(item => item.Id != parcel.Id).ToList();
        bool split = others.Any(item => item.BundleId is not null && item.BundleId != bundle.Id);

        if (shipment.Options.HasFlag(OperationsShipmentOptions.Consolidate))
        {
            if (shipment.WayOr(settings.ConsolidationWay) == OperationsConsolidationWay.Repack)
            {
                return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.RepackFirst);
            }

            if (split)
            {
                return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.KeptTogether);
            }
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        bool past;
        shipment.StartConsolidating(settings.ConsolidationWay, utcNow);

        try
        {
            past = bundle.Take(1, parcel.WeightKg ?? 0m, bundle.VolumetricOf(parcel.LengthCm ?? 0m, parcel.WidthCm ?? 0m, parcel.HeightCm ?? 0m), settings.BundleTargetRule, utcNow);
            parcel.Pack(bundle.Id, utcNow);
        }
        catch (DomainException exception)
        {
            return ScanIntoOperationsBundleResponse.Failure(exception.Error);
        }

        await _bundling.PackedAsync(parcel, bundle, userId, scan, cancellationToken);
        return ScanIntoOperationsBundleResponse.Success(parcel.Id, null, false, past, split);
    }

    // A repacked shipment's closed box: it goes in with all its parcels.
    private async Task<ScanIntoOperationsBundleResponse> BoxAsync(OperationsBundle bundle, OperationsRepackBox box, Vixly.Operations.Domain.Settings.OperationsTenantSettings settings, Guid userId,
        string scan, CancellationToken cancellationToken)
    {
        if (box.BundleId == bundle.Id)
        {
            return ScanIntoOperationsBundleResponse.Success(null, box.Id, true, false, false);
        }

        if (box.WarehouseId != bundle.WarehouseId)
        {
            return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.WrongWarehouse);
        }

        if (box.Status != OperationsRepackBoxStatus.Closed)
        {
            return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.BoxNotClosed);
        }

        if (box.BundleId is not null)
        {
            return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.InAnotherBundle);
        }

        if (await _receiving.GetShipmentAsync(box.ShipmentId, cancellationToken) is not { IsBooked: true } shipment)
        {
            return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.NotFinal);
        }

        if (bundle.Refuses(shipment) is { } wrong)
        {
            return ScanIntoOperationsBundleResponse.Failure(wrong);
        }

        if (!await _bundling.PaidAsync(shipment, cancellationToken))
        {
            return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.NotPaid);
        }

        List<OperationsParcel> inside = (await _receiving.ListShipmentParcelsForUpdateAsync(shipment.Id, cancellationToken)).Where(item => item.BoxId == box.Id).ToList();

        if (inside.Count == 0)
        {
            return ScanIntoOperationsBundleResponse.Failure(OperationsBundleErrors.BoxIncomplete);
        }

        if (inside.Select(item => item.BundleId is not null ? OperationsBundleErrors.InAnotherBundle : OperationsBundling.Ready(item, shipment)).FirstOrDefault(error => error is not null) is { } refused)
        {
            return ScanIntoOperationsBundleResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        bool past;
        shipment.StartConsolidating(settings.ConsolidationWay, utcNow);

        try
        {
            past = bundle.Take(inside.Count, box.WeightKg ?? 0m, bundle.VolumetricOf(box.LengthCm ?? 0m, box.WidthCm ?? 0m, box.HeightCm ?? 0m), settings.BundleTargetRule, utcNow);
            box.PutInto(bundle.Id, utcNow);

            foreach (OperationsParcel parcel in inside)
            {
                parcel.Pack(bundle.Id, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return ScanIntoOperationsBundleResponse.Failure(exception.Error);
        }

        foreach (OperationsParcel parcel in inside)
        {
            await _bundling.PackedAsync(parcel, bundle, userId, scan, cancellationToken);
        }

        return ScanIntoOperationsBundleResponse.Success(null, box.Id, false, past, false);
    }
}
