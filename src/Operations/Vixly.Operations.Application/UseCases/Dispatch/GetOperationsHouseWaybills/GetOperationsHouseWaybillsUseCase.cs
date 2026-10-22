using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Dispatch;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsHouseWaybills;

// =========================================================
// FILE: GetOperationsHouseWaybillsUseCase.cs
//
// PURPOSE:
// The house air waybills of a closed dispatch as one PDF, or of one of its shipments (Slice 6B step 2, D-6; owner, A-5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// =========================================================
public sealed class GetOperationsHouseWaybillsUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly OperationsDispatchPapers _papers;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly TimeProvider _timeProvider;

    public GetOperationsHouseWaybillsUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        OperationsDispatchPapers papers,
        IOperationsBrandRepository brands,
        IOperationsSettingsRepository settings,
        IOperationsLabelRenderer renderer,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _papers = papers ?? throw new ArgumentNullException(nameof(papers));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsHouseWaybillsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsHouseWaybillsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsHouseWaybillsResponse.Failure(refused);
        }

        if (await _dispatches.GetDispatchAsync(request.DispatchId, cancellationToken) is not { } dispatch || warehouses.All(item => item.Id != dispatch.WarehouseId))
        {
            return GetOperationsHouseWaybillsResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        if (dispatch.Status is OperationsDispatchStatus.Planned or OperationsDispatchStatus.Cancelled)
        {
            return GetOperationsHouseWaybillsResponse.Failure(OperationsDispatchErrors.NotClosed);
        }

        // House air waybills are for air (D-6).
        if (!dispatch.TravelsByAir)
        {
            return GetOperationsHouseWaybillsResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        Dictionary<Guid, OperationsCarrier> carriers = (await _dispatches.ListCarriersAsync(cancellationToken)).ToDictionary(carrier => carrier.Id);
        IReadOnlyList<OperationsHouseWaybill> waybills = await _papers.HouseWaybillsAsync(dispatch, request.ShipmentId, (await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty, carriers,
            settings, OperationsTimeZones.Today(dispatch.ClosedUtc ?? _timeProvider.GetUtcNow(), settings.TimeZone), request.ContentsFallback, cancellationToken);

        if (waybills.Count == 0)
        {
            return GetOperationsHouseWaybillsResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        return GetOperationsHouseWaybillsResponse.Success(_renderer.RenderHouseWaybills(waybills), (waybills.Count == 1 ? waybills[0].HouseNumber : dispatch.Number) + "-HAWB.pdf");
    }
}
