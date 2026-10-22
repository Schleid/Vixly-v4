using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsCarrier;

// =========================================================
// FILE: GetOperationsCarrierUseCase.cs
//
// PURPOSE:
// One carrier with its rates (Slice 6B, D-1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.carriers.manage (the company's carriers and their rates, D-8)
// =========================================================
public sealed class GetOperationsCarrierUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsCarrierUseCase(
        OperationsAccess access,
        IOperationsDispatchRepository dispatches,
        IOperationsLocationRepository locations,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsCarrierResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCarrierRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CarriersManage, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsCarrierResponse.Failure(refused);
        }

        if (await _dispatches.GetCarrierAsync(request.CarrierId, cancellationToken) is not { } carrier)
        {
            return GetOperationsCarrierResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        IReadOnlyList<Vixly.Operations.Domain.Locations.OperationsWarehouse> destinations = (await _locations.ListWarehousesAsync(cancellationToken)).Where(warehouse => warehouse.IsDestination)
            .OrderBy(warehouse => warehouse.Code, StringComparer.Ordinal).ToList();
        IReadOnlyList<OperationsCarrierRate> rates = (await _dispatches.ListRatesAsync(carrier.Id, cancellationToken)).OrderBy(rate => rate.CountryCode, StringComparer.Ordinal)
            .ThenBy(rate => rate.DestinationWarehouseId).ThenBy(rate => rate.BundleType, StringComparer.Ordinal).ThenByDescending(rate => rate.StartsOn).ToList();
        return GetOperationsCarrierResponse.Success(carrier, rates, destinations, settings.BundleTypeList(), settings.CurrencyCode, settings.WeightUnit,
            Vixly.Operations.Domain.Locations.OperationsTimeZones.Today(_timeProvider.GetUtcNow(), settings.TimeZone));
    }
}
