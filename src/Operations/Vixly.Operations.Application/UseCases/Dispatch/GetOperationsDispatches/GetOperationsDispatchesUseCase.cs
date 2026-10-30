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

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsDispatches;

// =========================================================
// FILE: GetOperationsDispatchesUseCase.cs
//
// PURPOSE:
// The dispatch desk (Slice 6B, D-2): the dispatches of a person's warehouses, and what a new one can be for.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// =========================================================
public sealed class GetOperationsDispatchesUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsShippingServiceRepository _services;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsDispatchesUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        IOperationsLocationRepository locations,
        IOperationsShippingServiceRepository services,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsDispatchesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsDispatchesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsDispatchesResponse.Failure(refused);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        bool all = string.Equals(request.View, "all", StringComparison.Ordinal);
        List<Guid> ids = warehouses.Select(item => item.Id).ToList();
        IReadOnlyList<OperationsDispatch> dispatches = await _dispatches.ListDispatchesAsync(ids, !all, 300, cancellationToken);
        IReadOnlyDictionary<Guid, int> free = await _dispatches.CountFreeBundlesAsync(ids, cancellationToken);

        return GetOperationsDispatchesResponse.Success(warehouses, (await _locations.ListWarehousesAsync(cancellationToken)).Where(item => item.IsDestination).OrderBy(item => item.Code, StringComparer.Ordinal).ToList(),
            dispatches, (await _services.ListAsync(cancellationToken)).Where(item => item.Offered).Select(item => item.ServiceCode).Order(StringComparer.Ordinal).ToList(), free, settings.TimeZone);
    }
}
