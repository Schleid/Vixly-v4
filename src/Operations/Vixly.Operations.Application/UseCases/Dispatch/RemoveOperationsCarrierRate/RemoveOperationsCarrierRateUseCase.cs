using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.RemoveOperationsCarrierRate;

// =========================================================
// FILE: RemoveOperationsCarrierRateUseCase.cs
//
// PURPOSE:
// Remove a rate that has not started yet (Slice 6B, D-1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.carriers.manage (the company's carriers and their rates, D-8)
// =========================================================
public sealed class RemoveOperationsCarrierRateUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RemoveOperationsCarrierRateUseCase(
        OperationsAccess access,
        IOperationsDispatchRepository dispatches,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RemoveOperationsCarrierRateResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RemoveOperationsCarrierRateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CarriersManage, false, cancellationToken);

        if (refused is not null)
        {
            return RemoveOperationsCarrierRateResponse.Failure(refused);
        }

        if (await _dispatches.GetRateAsync(request.RateId, cancellationToken) is not { } rate)
        {
            // Removed already (a double click): done.
            return RemoveOperationsCarrierRateResponse.Success();
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        if (rate.StartsOn <= Vixly.Operations.Domain.Locations.OperationsTimeZones.Today(_timeProvider.GetUtcNow(), settings.TimeZone))
        {
            return RemoveOperationsCarrierRateResponse.Failure(OperationsDispatchErrors.RateStarted);
        }

        _dispatches.Remove(rate);
        _audit.Record(access!.User.Id, "ops.carriers.rate_removed", nameof(OperationsCarrier), rate.CarrierId,
            before: new { country = rate.CountryCode, destination = rate.DestinationWarehouseId, type = rate.BundleType, perKg = rate.PerKg, minimum = rate.Minimum, perBundle = rate.PerBundle, starts = rate.StartsOn });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RemoveOperationsCarrierRateResponse.Success()
            : RemoveOperationsCarrierRateResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
