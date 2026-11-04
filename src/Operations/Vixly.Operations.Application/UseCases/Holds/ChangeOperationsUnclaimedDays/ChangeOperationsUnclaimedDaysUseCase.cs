using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Holds;
using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Holds.ChangeOperationsUnclaimedDays;

// =========================================================
// FILE: ChangeOperationsUnclaimedDaysUseCase.cs
//
// PURPOSE:
// Changes after how many days a waiting package goes to the decision list (Slice 3E, Q-R6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The Owner or an administrator; 30 to 180 days (the final notice goes out at day 25)
// =========================================================
public sealed class ChangeOperationsUnclaimedDaysUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsUnclaimedDaysUseCase(
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

    public async Task<ChangeOperationsUnclaimedDaysResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsUnclaimedDaysRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ChangeOperationsUnclaimedDaysResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (!(access.IsOwner || access.IsAdministrator))
        {
            return ChangeOperationsUnclaimedDaysResponse.Failure(OperationsHoldErrors.ApproverNotAllowed);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? saved = await _settings.FindAsync(cancellationToken);
        OperationsTenantSettings settings = saved ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
        int before = settings.UnclaimedDays;

        try
        {
            settings.ChangeUnclaimedDays(request.Days, utcNow);
        }
        catch (DomainException exception)
        {
            return ChangeOperationsUnclaimedDaysResponse.Failure(exception.Error);
        }

        if (saved is null)
        {
            _settings.Add(settings);
        }

        _audit.Record(access.User.Id, "ops.holds.unclaimed_days_changed", nameof(OperationsTenantSettings), settings.Id, before: new { days = before }, after: new { days = settings.UnclaimedDays });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ChangeOperationsUnclaimedDaysResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return (await _settings.FindAsync(cancellationToken))?.UnclaimedDays == request.Days
            ? ChangeOperationsUnclaimedDaysResponse.Success()
            : ChangeOperationsUnclaimedDaysResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
