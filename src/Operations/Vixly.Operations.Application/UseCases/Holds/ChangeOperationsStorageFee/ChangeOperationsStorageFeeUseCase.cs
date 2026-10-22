using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Holds.ChangeOperationsStorageFee;

// =========================================================
// FILE: ChangeOperationsStorageFeeUseCase.cs
//
// PURPOSE:
// Changes the days a package waits free of charge and the storage fee per day after them (Slice 4D step 3, Q-D3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The Owner or an administrator; 0 to 180 free days; no fee by default (Q-D3)
// =========================================================
public sealed class ChangeOperationsStorageFeeUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsStorageFeeUseCase(
        OperationsOwnerCheck owner,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsStorageFeeResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsStorageFeeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ChangeOperationsStorageFeeResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (!(access.IsOwner || access.IsAdministrator))
        {
            return ChangeOperationsStorageFeeResponse.Failure(OperationsHoldErrors.ApproverNotAllowed);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? saved = await _settings.FindAsync(cancellationToken);
        OperationsTenantSettings settings = saved ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
        object before = new { freeDays = settings.StorageFreeDays, fee = settings.StorageFeePerDay };

        try
        {
            settings.ChangeStorageFee(request.FreeDays, request.FeePerDay, utcNow);
        }
        catch (DomainException exception)
        {
            return ChangeOperationsStorageFeeResponse.Failure(exception.Error);
        }

        if (saved is null)
        {
            _settings.Add(settings);
        }

        _audit.Record(access.User.Id, "ops.holds.storage_fee_changed", nameof(OperationsTenantSettings), settings.Id, before: before,
            after: new { freeDays = settings.StorageFreeDays, fee = settings.StorageFeePerDay });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeOperationsStorageFeeResponse.Success()
            : ChangeOperationsStorageFeeResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
