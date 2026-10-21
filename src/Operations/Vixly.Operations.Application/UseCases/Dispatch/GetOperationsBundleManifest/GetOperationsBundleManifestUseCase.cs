using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Dispatch;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsBundleManifest;

// =========================================================
// FILE: GetOperationsBundleManifestUseCase.cs
//
// PURPOSE:
// A bundle's manifest as a PDF (Slice 6B, D-6): the shipments and parcels inside it.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.manage, ops.bundles.fill or ops.dispatches.manage at the bundle's warehouse
// =========================================================
public sealed class GetOperationsBundleManifestUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsBundleRepository _bundles;

    private readonly OperationsManifests _manifests;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly TimeProvider _timeProvider;

    public GetOperationsBundleManifestUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        IOperationsBundleRepository bundles,
        OperationsManifests manifests,
        IOperationsLocationRepository locations,
        IOperationsBrandRepository brands,
        IOperationsSettingsRepository settings,
        IOperationsLabelRenderer renderer,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _manifests = manifests ?? throw new ArgumentNullException(nameof(manifests));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsBundleManifestResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsBundleManifestRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context,
            new[] { OperationsPermissions.BundlesManage, OperationsPermissions.BundlesFill, OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsBundleManifestResponse.Failure(refused);
        }

        if (await _bundles.GetBundleAsync(request.BundleId, cancellationToken) is not { } bundle || warehouses.FirstOrDefault(item => item.Id == bundle.WarehouseId) is not { } warehouse
            || bundle.Status == OperationsBundleStatus.Voided)
        {
            return GetOperationsBundleManifestResponse.Failure(OperationsBundleErrors.NotFound);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        OperationsWarehouse? destination = bundle.DestinationWarehouseId is { } destinationId ? await _locations.GetWarehouseAsync(destinationId, cancellationToken) : null;
        string? dispatchNumber = bundle.DispatchId is { } dispatchId ? (await _dispatches.GetDispatchAsync(dispatchId, cancellationToken))?.Number : null;
        OperationsBundleManifest manifest = new((await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty, warehouse.Code, destination?.Code ?? bundle.DestinationCountryCode,
            bundle.ServiceCode, dispatchNumber, await _manifests.BundleAsync(bundle, cancellationToken), settings.WeightUnit, settings.DimensionUnit);
        return GetOperationsBundleManifestResponse.Success(_renderer.RenderBundleManifest(manifest), bundle.Number);
    }
}
