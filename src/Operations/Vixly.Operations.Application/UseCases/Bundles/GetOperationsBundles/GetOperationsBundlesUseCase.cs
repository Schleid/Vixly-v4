using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundles;

// =========================================================
// FILE: GetOperationsBundlesUseCase.cs
//
// PURPOSE:
// The bundles desk (Slice 6A, D-10): parcels ready to bundle by destination and service, the open and closed bundles.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.manage or ops.bundles.fill, in the warehouses where the person holds one
// - Ready: final, paid, clear parcels in no bundle; waiting: final but unpaid or held
// =========================================================
public sealed class GetOperationsBundlesUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsShippingServiceRepository _services;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsBundlesUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        IOperationsBillingRepository billing,
        IOperationsLocationRepository locations,
        IOperationsShippingServiceRepository services,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsBundlesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsBundlesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesManage, OperationsPermissions.BundlesFill }, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsBundlesResponse.Failure(refused);
        }
        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        List<Guid> ids = warehouses.Select(warehouse => warehouse.Id).ToList();
        IReadOnlyList<(OperationsParcel Parcel, OperationsShipment Shipment)> ready = await _bundles.ListReadyAsync(ids, 3000, cancellationToken);
        HashSet<Guid> unpaid = (await _billing.ListShipmentInvoicesAsync(ready.Select(item => item.Shipment.Id).Distinct().ToList(), cancellationToken))
            .Where(invoice => invoice.ShipmentId is not null).GroupBy(invoice => invoice.ShipmentId!.Value).Where(group => OperationsBundling.Unpaid(group)).Select(group => group.Key).ToHashSet();

        // A repacked shipment's parcels go in only inside their closed box (review): loose or in an open box, they wait.
        HashSet<Guid> closedBoxes = (await _bundles.ListBoxesAsync(ids, 1000, cancellationToken)).Where(box => box.Status == OperationsRepackBoxStatus.Closed && box.BundleId is null)
            .Select(box => box.Id).ToHashSet();
        bool Bundleable((OperationsParcel Parcel, OperationsShipment Shipment) item) =>
            OperationsBundling.Ready(item.Parcel, item.Shipment) is null && !unpaid.Contains(item.Shipment.Id)
            && (!item.Shipment.Options.HasFlag(OperationsShipmentOptions.Consolidate) || item.Shipment.WayOr(settings.ConsolidationWay) != OperationsConsolidationWay.Repack
                || (item.Parcel.BoxId is { } box && closedBoxes.Contains(box)));

        List<OperationsReadyGroup> groups = ready
            .GroupBy(item => (Warehouse: item.Parcel.WarehouseId ?? Guid.Empty, Destination: OperationsBundle.DestinationOf(item.Shipment), Country: item.Shipment.DestinationCountryCode ?? string.Empty,
                Service: item.Shipment.ServiceCode ?? string.Empty))
            .Select(group =>
            {
                List<(OperationsParcel Parcel, OperationsShipment Shipment)> clear = group
                    .Where(Bundleable).ToList();
                return new OperationsReadyGroup(group.Key.Warehouse, group.Key.Destination.WarehouseId, group.Key.Country, group.Key.Service, clear.Count,
                    clear.Select(item => item.Shipment.Id).Distinct().Count(), clear.Sum(item => item.Parcel.WeightKg ?? 0m), clear.Sum(item => OperationsBundling.VolumeCm3(item.Parcel)),
                    group.Count() - clear.Count, clear.Count(item => item.Shipment.Options.HasFlag(OperationsShipmentOptions.Consolidate)));
            })
            .OrderBy(group => group.WarehouseId).ThenBy(group => group.CountryCode, StringComparer.Ordinal).ThenBy(group => group.ServiceCode, StringComparer.Ordinal).ToList();

        IReadOnlyList<Vixly.Operations.Domain.Settings.OperationsShippingServiceSetup> services = (await _services.ListAsync(cancellationToken)).Where(service => service.Offered).ToList();

        return GetOperationsBundlesResponse.Success(warehouses, await _locations.ListWarehousesAsync(cancellationToken), groups, await _bundles.ListBundlesAsync(ids, 200, cancellationToken),
            services.Select(service => service.ServiceCode).ToList(), settings.BundleTypeList(),
            warehouses.Where(warehouse => OperationsBundleDesk.Can(access!, OperationsPermissions.BundlesManage, warehouse)).Select(warehouse => warehouse.Id).ToList(), settings.WeightUnit,
            services.ToDictionary(service => service.ServiceCode, service => service.VolumetricDivisor, StringComparer.Ordinal));
    }
}
