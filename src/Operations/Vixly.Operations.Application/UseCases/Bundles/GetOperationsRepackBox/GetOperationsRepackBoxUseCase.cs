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

namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsRepackBox;

// =========================================================
// FILE: GetOperationsRepackBoxUseCase.cs
//
// PURPOSE:
// One repack box (Slice 6A, D-4): its shipment's parcels, in the box or still to add, with their shelves.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.manage or ops.bundles.fill, at its warehouse
// - Another warehouse's box gives the same answer as a missing one (S-3)
// =========================================================
public sealed class GetOperationsRepackBoxUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsRepackBoxUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        IOperationsReceivingRepository receiving,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsRepackBoxResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsRepackBoxRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesManage, OperationsPermissions.BundlesFill }, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsRepackBoxResponse.Failure(refused);
        }

        if (await _bundles.GetBoxAsync(request.BoxId, cancellationToken) is not { } box || warehouses.FirstOrDefault(item => item.Id == box.WarehouseId) is not { } warehouse
            || await _receiving.GetShipmentAsync(box.ShipmentId, cancellationToken) is not { } shipment)
        {
            return GetOperationsRepackBoxResponse.Failure(OperationsBundleErrors.NotFound);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        IReadOnlyList<OperationsParcel> parcels = OperationsShipmentBooking.Counted(await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken));
        Dictionary<Guid, string> shelves = (await _receiving.ListLocationsByIdAsync(parcels.Where(parcel => parcel.StorageLocationId is not null).Select(parcel => parcel.StorageLocationId!.Value)
            .Distinct().ToList(), cancellationToken)).ToDictionary(location => location.Id, location => location.Code);
        string? bundleNumber = box.BundleId is { } bundleId ? (await _bundles.ListBundlesByIdAsync(new[] { bundleId }, cancellationToken)).FirstOrDefault()?.Number : null;

        return GetOperationsRepackBoxResponse.Success(box, shipment, warehouse, parcels, shelves, bundleNumber, OperationsBundleDesk.Can(access!, OperationsPermissions.BundlesManage, warehouse),
            OperationsBundleDesk.Can(access!, OperationsPermissions.BundlesFill, warehouse), settings.WeightUnit, settings.DimensionUnit);
    }
}
