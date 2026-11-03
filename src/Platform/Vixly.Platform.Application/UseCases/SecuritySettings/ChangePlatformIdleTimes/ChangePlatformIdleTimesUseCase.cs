using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SecuritySettings.ChangePlatformIdleTimes;

// =========================================================
// FILE: ChangePlatformIdleTimesUseCase.cs
//
// PURPOSE:
// Changes Control's inactivity sign-out and the warning before it (owner, 2026-10-27: "a setting the security administrator manages").
//
// RESPONSIBILITIES:
// - The security administrator's right over sessions (platform.users.end_sessions), with a fresh confirmation (step-up)
// - Check the times; record the change, before and after, in the history
// - Forget the kept times so the change applies at once on this server
//
// IMPORTANT NOTES:
// - Sign-out after 5 to 30 minutes; the warning 1 to 5 minutes before it
// =========================================================
public sealed class ChangePlatformIdleTimesUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformSecuritySettingsRepository _settings;

    private readonly IPlatformIdleTimesReader _idle;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangePlatformIdleTimesUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformStepUpGuard stepUp,
        IPlatformSecuritySettingsRepository settings,
        IPlatformIdleTimesReader idle,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _idle = idle ?? throw new ArgumentNullException(nameof(idle));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangePlatformIdleTimesResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ChangePlatformIdleTimesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.UsersEndSessions, cancellationToken);

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        PlatformSecuritySettings? settings = await _settings.GetAsync(cancellationToken);
        PlatformSecuritySettings current = settings ?? PlatformSecuritySettings.Default(utcNow);

        if (current.IdleMinutes == request.IdleMinutes && current.IdleWarnMinutes == request.IdleWarnMinutes)
        {
            return ChangePlatformIdleTimesResponse.Success();
        }

        if (!await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken))
        {
            return ChangePlatformIdleTimesResponse.Failure(PlatformCommonErrors.StepUpRequired);
        }

        object before = new { idleMinutes = current.IdleMinutes, warnMinutes = current.IdleWarnMinutes };

        try
        {
            current.ChangeIdle(request.IdleMinutes, request.IdleWarnMinutes, context.PlatformUserId, utcNow);
        }
        catch (DomainException exception)
        {
            return ChangePlatformIdleTimesResponse.Failure(exception.Error);
        }

        if (settings is null)
        {
            _settings.Add(current);
        }

        _audit.Record(
            context.PlatformUserId,
            "platform.security.idle_changed",
            PlatformPermissions.UsersEndSessions,
            nameof(PlatformSecuritySettings),
            current.Id,
            before: before,
            after: new { idleMinutes = current.IdleMinutes, warnMinutes = current.IdleWarnMinutes });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ChangePlatformIdleTimesResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        _idle.Forget();
        return ChangePlatformIdleTimesResponse.Success();
    }
}
