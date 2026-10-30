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

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsDispatch;

// =========================================================
// FILE: GetOperationsDispatchUseCase.cs
//
// PURPOSE:
// One dispatch (Slice 6B, D-2 to D-4): its bundles, the bundles that fit it, its legs and the carriers.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// =========================================================
public sealed class GetOperationsDispatchUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly OperationsDispatchPapers _papers;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsDispatchUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        OperationsDispatchPapers papers,
        IOperationsLocationRepository locations,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _papers = papers ?? throw new ArgumentNullException(nameof(papers));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsDispatchResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsDispatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsDispatchResponse.Failure(refused);
        }

        if (await _dispatches.GetDispatchAsync(request.DispatchId, cancellationToken) is not { } dispatch || warehouses.All(item => item.Id != dispatch.WarehouseId))
        {
            return GetOperationsDispatchResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        IReadOnlyList<OperationsBundle> bundles = await _dispatches.ListDispatchBundlesAsync(dispatch.Id, cancellationToken);
        IReadOnlyList<OperationsBundle> candidates = dispatch.Status == OperationsDispatchStatus.Planned
            ? await _dispatches.ListFittingBundlesAsync(dispatch.WarehouseId, dispatch.DestinationWarehouseId, dispatch.DestinationCountryCode, dispatch.ServiceCode, 200, cancellationToken)
            : Array.Empty<OperationsBundle>();
        OperationsWarehouse? destination = dispatch.DestinationWarehouseId is { } destinationId ? await _locations.GetWarehouseAsync(destinationId, cancellationToken) : null;
        return GetOperationsDispatchResponse.Success(dispatch, warehouses.First(item => item.Id == dispatch.WarehouseId), destination, bundles.OrderBy(bundle => bundle.Number, StringComparer.Ordinal).ToList(),
            candidates.OrderBy(bundle => bundle.Number, StringComparer.Ordinal).ToList(), await _dispatches.ListCarriersAsync(cancellationToken), settings.WeightUnit, settings.TimeZone,
            await _papers.RowsAsync(dispatch, cancellationToken));
    }
}
