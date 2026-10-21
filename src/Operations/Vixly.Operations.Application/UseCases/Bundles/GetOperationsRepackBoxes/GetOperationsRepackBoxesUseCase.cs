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

namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsRepackBoxes;

// =========================================================
// FILE: GetOperationsRepackBoxesUseCase.cs
//
// PURPOSE:
// The consolidated shipments waiting to travel and their repack boxes (Slice 6A, D-4 and D-10).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.manage or ops.bundles.fill
// - Only shipments with parcels still to bundle
// =========================================================
public sealed class GetOperationsRepackBoxesUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsRepackBoxesUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        IOperationsBillingRepository billing,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsRepackBoxesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsRepackBoxesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesManage, OperationsPermissions.BundlesFill }, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsRepackBoxesResponse.Failure(refused);
        }
        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        List<Guid> ids = warehouses.Select(warehouse => warehouse.Id).ToList();
        List<(OperationsParcel Parcel, OperationsShipment Shipment)> ready = (await _bundles.ListReadyAsync(ids, 3000, cancellationToken))
            .Where(item => item.Shipment.Options.HasFlag(OperationsShipmentOptions.Consolidate)).ToList();
        HashSet<Guid> unpaid = (await _billing.ListShipmentInvoicesAsync(ready.Select(item => item.Shipment.Id).Distinct().ToList(), cancellationToken))
            .Where(invoice => invoice.ShipmentId is not null).GroupBy(invoice => invoice.ShipmentId!.Value).Where(group => OperationsBundling.Unpaid(group)).Select(group => group.Key).ToHashSet();
        Dictionary<Guid, OperationsRepackBox> boxes = (await _bundles.ListBoxesAsync(ids, 500, cancellationToken)).Where(box => box.BundleId is null)
            .GroupBy(box => box.ShipmentId).ToDictionary(group => group.Key, group => group.First());

        List<OperationsRepackShipmentRow> rows = ready.GroupBy(item => item.Shipment.Id).Select(group =>
        {
            OperationsShipment shipment = group.First().Shipment;
            OperationsRepackBox? box = boxes.GetValueOrDefault(shipment.Id);
            return new OperationsRepackShipmentRow(shipment.Id, shipment.Number, shipment.OriginWarehouseId, group.Count(), shipment.WayOr(settings.ConsolidationWay), shipment.ConsolidationWay is not null,
                box?.Id, box?.Number, box?.Status, !unpaid.Contains(shipment.Id));
        }).OrderBy(row => row.Number, StringComparer.Ordinal).ToList();

        return GetOperationsRepackBoxesResponse.Success(rows, warehouses,
            warehouses.Where(warehouse => OperationsBundleDesk.Can(access!, OperationsPermissions.BundlesManage, warehouse)).Select(warehouse => warehouse.Id).ToList(), settings.ConsolidationWay,
            warehouses.Where(warehouse => OperationsBundleDesk.Can(access!, OperationsPermissions.BundlesFill, warehouse)).Select(warehouse => warehouse.Id).ToList());
    }
}
