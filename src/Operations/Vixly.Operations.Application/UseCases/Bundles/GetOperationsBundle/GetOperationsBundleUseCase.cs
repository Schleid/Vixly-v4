using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundle;

// =========================================================
// FILE: GetOperationsBundleUseCase.cs
//
// PURPOSE:
// One bundle (Slice 6A, D-10): its parcels, its shipments' completeness, and the shipment of the last scan.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.manage or ops.bundles.fill, at its warehouse
// - Another warehouse's bundle gives the same answer as a missing one (S-3)
// =========================================================
public sealed class GetOperationsBundleUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsLocationRepository _locations;

    private readonly OperationsBundleShipments _shipments;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsBundleUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        IOperationsTrackingRepository tracking,
        IOperationsLocationRepository locations,
        OperationsBundleShipments shipments,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _shipments = shipments ?? throw new ArgumentNullException(nameof(shipments));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsBundleResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsBundleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesManage, OperationsPermissions.BundlesFill }, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsBundleResponse.Failure(refused);
        }

        // Another warehouse's bundle gives the same answer as a missing one (S-3).
        if (await _bundles.GetBundleAsync(request.BundleId, cancellationToken) is not { } bundle || warehouses.FirstOrDefault(item => item.Id == bundle.WarehouseId) is not { } warehouse)
        {
            return GetOperationsBundleResponse.Failure(OperationsBundleErrors.NotFound);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        IReadOnlyList<OperationsParcel> parcels = await _bundles.ListBundleParcelsForUpdateAsync(bundle.Id, cancellationToken);
        IReadOnlyList<OperationsBundleShipmentLine> lines = await _shipments.DescribeAsync(parcels.Where(parcel => parcel.ShipmentId is not null).Select(parcel => parcel.ShipmentId!.Value), bundle.Id,
            cancellationToken);
        Dictionary<Guid, string> boxes = (await _bundles.ListBundleBoxesForUpdateAsync(bundle.Id, cancellationToken)).ToDictionary(box => box.Id, box => box.Number);
        Dictionary<Guid, string?> numbers = lines.ToDictionary(line => line.ShipmentId, line => line.Number);
        List<OperationsBundleParcelRow> rows = parcels.OrderByDescending(parcel => parcel.LastEventUtc ?? parcel.CreatedUtc).Select(parcel => new OperationsBundleParcelRow(parcel.Id, parcel.Number,
            parcel.ShipmentId, parcel.ShipmentId is { } shipmentId ? numbers.GetValueOrDefault(shipmentId) : null, parcel.WeightKg, parcel.BoxId,
            parcel.BoxId is { } boxId ? boxes.GetValueOrDefault(boxId) : null)).ToList();

        OperationsBundleShipmentLine? focus = null;

        if (request.FocusParcelId is { } focusId && await _tracking.GetParcelAsync(focusId, cancellationToken) is { ShipmentId: { } focusShipment } focused
            && focused.WarehouseId == bundle.WarehouseId)
        {
            focus = lines.FirstOrDefault(line => line.ShipmentId == focusShipment) ?? (await _shipments.DescribeAsync(new[] { focusShipment }, bundle.Id, cancellationToken)).FirstOrDefault();
        }

        OperationsWarehouse? destination = bundle.DestinationWarehouseId is { } destinationId ? await _locations.GetWarehouseAsync(destinationId, cancellationToken) : null;
        return GetOperationsBundleResponse.Success(bundle, warehouse, destination, rows, lines, focus, OperationsBundleDesk.Can(access!, OperationsPermissions.BundlesManage, warehouse),
            OperationsBundleDesk.Can(access!, OperationsPermissions.BundlesFill, warehouse), settings.WeightUnit, settings.DimensionUnit,
            bundle.TargetKg is { } target ? OperationsUnits.ToShownWeight(target, settings.WeightUnit) : null);
    }
}
