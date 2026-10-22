using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.AddOperationsCarrierRate;

// =========================================================
// FILE: AddOperationsCarrierRateUseCase.cs
//
// PURPOSE:
// Add a carrier's rate to a destination from a day on (Slice 6B, D-1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.carriers.manage (the company's carriers and their rates, D-8)
// - A later rate replaces an earlier one from its start; a rate is never edited
// =========================================================
public sealed class AddOperationsCarrierRateUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AddOperationsCarrierRateUseCase(
        OperationsAccess access,
        IOperationsDispatchRepository dispatches,
        IOperationsLocationRepository locations,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AddOperationsCarrierRateResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        AddOperationsCarrierRateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CarriersManage, false, cancellationToken);

        if (refused is not null)
        {
            return AddOperationsCarrierRateResponse.Failure(refused);
        }

        if (await _dispatches.GetCarrierAsync(request.CarrierId, cancellationToken) is not { } carrier)
        {
            return AddOperationsCarrierRateResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        // A rate starts today or later: what was charged before stays as it was.
        if (request.StartsOn < Vixly.Operations.Domain.Locations.OperationsTimeZones.Today(utcNow, settings.TimeZone))
        {
            return AddOperationsCarrierRateResponse.Failure(OperationsDispatchErrors.RateInvalid);
        }

        string? country = request.CountryCode;

        if (request.DestinationWarehouseId is { } destinationId)
        {
            if (await _locations.GetWarehouseAsync(destinationId, cancellationToken) is not { IsDestination: true } destination)
            {
                return AddOperationsCarrierRateResponse.Failure(OperationsDispatchErrors.DestinationRequired);
            }

            country = destination.Address.CountryCode;
        }

        string? type = string.IsNullOrWhiteSpace(request.BundleType) ? null
            : settings.BundleTypeList().FirstOrDefault(item => string.Equals(item, request.BundleType.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.BundleType) && type is null)
        {
            return AddOperationsCarrierRateResponse.Failure(OperationsDispatchErrors.RateInvalid);
        }

        // Stored per kilogram.
        decimal? perKg = request.PerWeight is { } perWeight ? decimal.Round(perWeight / OperationsUnits.ToKilograms(1m, settings.WeightUnit), 4) : null;
        OperationsCarrierRate rate;

        try
        {
            rate = OperationsCarrierRate.Create(context.RequiredTenantId, carrier, request.DestinationWarehouseId, country, type, perKg, request.Minimum, request.PerBundle, request.StartsOn, utcNow);
        }
        catch (DomainException exception)
        {
            return AddOperationsCarrierRateResponse.Failure(exception.Error);
        }

        _dispatches.Add(rate);
        _audit.Record(access!.User.Id, "ops.carriers.rate_added", nameof(OperationsCarrier), carrier.Id,
            after: new { country = rate.CountryCode, destination = rate.DestinationWarehouseId, type = rate.BundleType, perKg = rate.PerKg, minimum = rate.Minimum, perBundle = rate.PerBundle, starts = rate.StartsOn });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? AddOperationsCarrierRateResponse.Success(rate.Id)
            : AddOperationsCarrierRateResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
