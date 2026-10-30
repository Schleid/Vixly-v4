using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.CloseOperationsBundle;

// =========================================================
// FILE: CloseOperationsBundleUseCase.cs
//
// PURPOSE:
// Close a bundle with its weight and size (Slice 6A, D-7).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.manage or ops.bundles.fill, at its warehouse (the person weighing it)
// - Never reopened afterwards (owner, A-4)
// =========================================================
public sealed class CloseOperationsBundleUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsBundling _bundling;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CloseOperationsBundleUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        IOperationsReceivingRepository receiving,
        OperationsBundling bundling,
        IOperationsSettingsRepository settings,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _bundling = bundling ?? throw new ArgumentNullException(nameof(bundling));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CloseOperationsBundleResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CloseOperationsBundleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesManage, OperationsPermissions.BundlesFill }, cancellationToken);

        if (refused is not null)
        {
            return CloseOperationsBundleResponse.Failure(refused);
        }

        if (await _bundles.GetBundleAsync(request.BundleId, cancellationToken) is not { } bundle || warehouses.All(item => item.Id != bundle.WarehouseId))
        {
            return CloseOperationsBundleResponse.Failure(OperationsBundleErrors.NotFound);
        }

        // Closed already (a double click): done.
        if (bundle.Status == OperationsBundleStatus.Closed)
        {
            return CloseOperationsBundleResponse.Success();
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        // Everything on the bundle's list scanned first (owner, 2026-10-21).
        if (await _bundles.AnyMissingAsync(bundle.Id, cancellationToken))
        {
            return CloseOperationsBundleResponse.Failure(OperationsBundleErrors.ItemsMissing);
        }

        // A kept-together shipment travels whole: closed with only part of it, the rest could never follow (review).
        foreach (Guid shipmentId in (await _bundles.ListBundleParcelsForUpdateAsync(bundle.Id, cancellationToken)).Where(parcel => parcel.ShipmentId is not null)
            .Select(parcel => parcel.ShipmentId!.Value).Distinct())
        {
            if (await _receiving.GetShipmentAsync(shipmentId, cancellationToken) is { } shipment && shipment.Options.HasFlag(Vixly.Operations.Domain.Shipments.OperationsShipmentOptions.Consolidate)
                && shipment.WayOr(settings.ConsolidationWay) == OperationsConsolidationWay.KeepTogether
                && Vixly.Operations.Application.Shipments.OperationsShipmentBooking.Counted(await _receiving.ListShipmentParcelsAsync(shipmentId, cancellationToken)).Any(parcel => parcel.BundleId != bundle.Id))
            {
                return CloseOperationsBundleResponse.Failure(OperationsBundleErrors.KeptTogetherIncomplete);
            }
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            bundle.Close(OperationsUnits.ToKilograms(request.Weight, settings.WeightUnit), OperationsUnits.ToCentimetres(request.Length, settings.DimensionUnit),
                OperationsUnits.ToCentimetres(request.Width, settings.DimensionUnit), OperationsUnits.ToCentimetres(request.Height, settings.DimensionUnit), access!.User.Id, utcNow);
        }
        catch (DomainException exception)
        {
            return CloseOperationsBundleResponse.Failure(exception.Error);
        }

        await _bundling.BundleEventAsync(bundle, OperationsTrackingCodes.BundleClosed, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["weight"] = OperationsBundling.Kilograms(bundle.WeightKg!.Value),
            ["size"] = OperationsBundling.Size(bundle.LengthCm!.Value, bundle.WidthCm!.Value, bundle.HeightCm!.Value)
        }, access.User.Id, null, cancellationToken);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CloseOperationsBundleResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return CloseOperationsBundleResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
