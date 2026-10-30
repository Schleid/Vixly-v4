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

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsDispatchManifest;

// =========================================================
// FILE: GetOperationsDispatchManifestUseCase.cs
//
// PURPOSE:
// The dispatch manifest as a PDF (Slice 6B, D-6): legs, bundles, shipments and consignees, for the carrier and customs.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// =========================================================
public sealed class GetOperationsDispatchManifestUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly OperationsManifests _manifests;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly TimeProvider _timeProvider;

    public GetOperationsDispatchManifestUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        OperationsManifests manifests,
        IOperationsLocationRepository locations,
        IOperationsBrandRepository brands,
        IOperationsSettingsRepository settings,
        IOperationsLabelRenderer renderer,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _manifests = manifests ?? throw new ArgumentNullException(nameof(manifests));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsDispatchManifestResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsDispatchManifestRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsDispatchManifestResponse.Failure(refused);
        }

        if (await _dispatches.GetDispatchAsync(request.DispatchId, cancellationToken) is not { } dispatch || warehouses.All(item => item.Id != dispatch.WarehouseId))
        {
            return GetOperationsDispatchManifestResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        Dictionary<Guid, string> carriers = (await _dispatches.ListCarriersAsync(cancellationToken)).ToDictionary(carrier => carrier.Id, carrier => carrier.Name);
        IReadOnlyList<OperationsManifestBundle> bundles = await _manifests.DispatchAsync(dispatch.Id,
            (await _dispatches.ListDispatchBundlesAsync(dispatch.Id, cancellationToken)).OrderBy(bundle => bundle.Number, StringComparer.Ordinal).ToList(), cancellationToken);

        OperationsWarehouse? destination = dispatch.DestinationWarehouseId is { } destinationId ? await _locations.GetWarehouseAsync(destinationId, cancellationToken) : null;
        OperationsDispatchManifest manifest = new((await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty, dispatch.Number, warehouses.First(item => item.Id == dispatch.WarehouseId).Code,
            destination?.Code ?? dispatch.DestinationCountryCode, dispatch.ServiceCode, dispatch.Status == OperationsDispatchStatus.Planned, dispatch.CutoffUtc,
            dispatch.Legs.OrderBy(leg => leg.Sequence).Select(leg => new OperationsManifestLeg(leg.Sequence, OperationsDispatching.Mode(leg.Kind), leg.CarrierId is { } id ? carriers.GetValueOrDefault(id) : null,
                leg.From, leg.To, leg.Reference, leg.DepartedUtc ?? leg.ScheduledDepartureUtc, leg.ArrivedUtc ?? leg.ScheduledArrivalUtc)).ToList(),
            bundles, settings.WeightUnit, settings.DimensionUnit, settings.TimeZone);
        return GetOperationsDispatchManifestResponse.Success(_renderer.RenderDispatchManifest(manifest), dispatch.Number);
    }
}
