using Vixly.Contracts.Platform.Catalog;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.SaveOperationsChargeExtra;

// =========================================================
// FILE: SaveOperationsChargeExtraUseCase.cs
//
// PURPOSE:
// Adds an extra charge or changes one; a new amount can be scheduled from a date (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SaveOperationsChargeExtraUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsChargeRepository _charges;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsChargeExtraUseCase(
        OperationsAccess access,
        IOperationsChargeRepository charges,
        IOperationsLocationRepository locations,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsChargeExtraResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsChargeExtraRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ChargesConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsChargeExtraResponse.Failure(refused);
        }

        if (request.Amount is not { } amount)
        {
            return SaveOperationsChargeExtraResponse.Failure(OperationsChargeErrors.PriceInvalid);
        }

        if (!string.IsNullOrWhiteSpace(request.ServiceCode) && PlatformShippingServices.Find(request.ServiceCode.Trim()) is null)
        {
            return SaveOperationsChargeExtraResponse.Failure(OperationsChargeErrors.ServiceUnknown);
        }

        // Slice 4C: the route names the company's own origins and destinations.
        IReadOnlyList<OperationsWarehouse> warehouses = await _locations.ListWarehousesAsync(cancellationToken);

        if ((request.OriginWarehouseId is { } originId && !warehouses.Any(warehouse => warehouse.Id == originId && warehouse.IsOrigin))
            || (request.DestinationWarehouseId is { } destinationId && !warehouses.Any(warehouse => warehouse.Id == destinationId && warehouse.IsDestination)))
        {
            return SaveOperationsChargeExtraResponse.Failure(OperationsChargeErrors.RouteInvalid);
        }

        OperationsExtraTerms terms = new(request.Name, request.IsGovernmentFee, request.Trigger, request.Words, request.ServiceCode, request.CountryCode, request.Mode,
            request.OriginWarehouseId, request.DestinationWarehouseId, request.Delivery);
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
        IReadOnlyList<OperationsChargeExtra> all = await _charges.ListExtrasAsync(cancellationToken);
        OperationsChargeExtra extra;

        try
        {
            DateTimeOffset? start = OperationsCharging.Start(request.EffectiveFrom, settings, utcNow);

            if (request.ExtraId is { } extraId)
            {
                extra = await _charges.GetExtraAsync(extraId, cancellationToken) ?? throw new DomainException(OperationsChargeErrors.ExtraNotFound);
                extra.Change(terms, request.Active, utcNow);

                // A new amount, or the same one from a later day: a new version; the amount the form showed, from now: nothing to schedule.
                if (start is not null || (extra.VersionAt(utcNow) ?? extra.Versions.FirstOrDefault()) is not { } current || current.Amount != amount || current.Calculation != request.Calculation || current.Per != request.Per)
                {
                    extra.Schedule(amount, request.Calculation, request.Per, start, utcNow);
                }
            }
            else
            {
                if (all.Count >= OperationsChargeLimits.MaxExtras)
                {
                    return SaveOperationsChargeExtraResponse.Failure(OperationsChargeErrors.TooMany);
                }

                extra = OperationsChargeExtra.Create(context.RequiredTenantId, terms, amount, request.Calculation, request.Per, start, utcNow);
                _charges.Add(extra);
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsChargeExtraResponse.Failure(exception.Error);
        }

        if (all.Any(other => other.Id != extra.Id && string.Equals(other.Name, extra.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return SaveOperationsChargeExtraResponse.Failure(OperationsChargeErrors.ExtraDuplicate);
        }

        OperationsExtraVersion? latest = extra.Versions.LastOrDefault();
        _audit.Record(access!.User.Id, "ops.charges.extra_saved", nameof(OperationsChargeExtra), extra.Id, after: new
        {
            name = extra.Name, government = extra.IsGovernmentFee, trigger = extra.Trigger.ToString(), words = extra.Words, service = extra.ServiceCode, country = extra.CountryCode,
            mode = extra.Mode?.ToString(), origin = extra.OriginWarehouseId, destination = extra.DestinationWarehouseId, delivery = extra.Delivery.ToString(), active = extra.IsActive, amount = latest?.Amount, calculation = latest?.Calculation.ToString(), per = latest?.Per.ToString(),
            effectiveFrom = latest?.EffectiveFromUtc
        });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SaveOperationsChargeExtraResponse.Success(extra.Id);
        }

        _unitOfWork.DiscardChanges();
        return request.ExtraId is null && (await _charges.ListExtrasAsync(cancellationToken)).FirstOrDefault(other => other.Name == extra.Name) is { } raced
            ? SaveOperationsChargeExtraResponse.Success(raced.Id)
            : SaveOperationsChargeExtraResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
