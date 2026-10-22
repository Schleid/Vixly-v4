using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.ScheduleOperationsSupplyPrice;

// =========================================================
// FILE: ScheduleOperationsSupplyPriceUseCase.cs
//
// PURPOSE:
// A new price for a supply, from now or a later day (Slice 5E, S-2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.charges.configure: the people who set prices manage supplies (Slice 5E, S-10)
// - A started price never changes: a new one replaces it from its day (Slice 4B rules)
// =========================================================
public sealed class ScheduleOperationsSupplyPriceUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ScheduleOperationsSupplyPriceUseCase(
        OperationsAccess access,
        IOperationsSupplyRepository supplies,
        IOperationsLocationRepository locations,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ScheduleOperationsSupplyPriceResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ScheduleOperationsSupplyPriceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ChargesConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return ScheduleOperationsSupplyPriceResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        if (await _supplies.GetAsync(request.SupplyId, cancellationToken) is not { } supply)
        {
            return ScheduleOperationsSupplyPriceResponse.Failure(OperationsSupplyErrors.NotFound);
        }

        OperationsSupplyPrice price;

        try
        {
            price = await OperationsSupplyPricing.PriceAsync(request.Price, request.Breaks, request.WarehousePrices, request.EffectiveFrom,
                _settings, _locations, context.RequiredTenantId, utcNow, cancellationToken);
            supply.SchedulePrice(price, utcNow);
        }
        catch (DomainException exception)
        {
            return ScheduleOperationsSupplyPriceResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.supplies.price_scheduled", nameof(OperationsSupply), supply.Id,
            after: new { code = supply.Code, price = price.Price, from = price.EffectiveFromUtc, breaks = price.Breaks.Count, warehouses = price.Warehouses.Count });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ScheduleOperationsSupplyPriceResponse.Success()
            : ScheduleOperationsSupplyPriceResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
