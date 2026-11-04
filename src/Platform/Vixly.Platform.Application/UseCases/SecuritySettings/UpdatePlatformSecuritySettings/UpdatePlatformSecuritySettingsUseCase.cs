using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SecuritySettings.UpdatePlatformSecuritySettings;

// =========================================================
// FILE: UpdatePlatformSecuritySettingsUseCase.cs
//
// PURPOSE:
// Turns Require MFA on or off for Control (owner, 2026-10-22:
// "Require MFA: Yes/No", top administrators only, confirmed).
//
// RESPONSIBILITIES:
// - SuperUser only (platform.users.change_authority, which no
//   role carries), with a fresh confirmation (step-up)
// - Record the change, before and after, in the history
//
// IMPORTANT NOTES:
// - No: people without a second factor sign in with the
//   password; those with one keep using it
// =========================================================
public sealed class UpdatePlatformSecuritySettingsUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformSecuritySettingsRepository _settings;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public UpdatePlatformSecuritySettingsUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformStepUpGuard stepUp,
        IPlatformSecuritySettingsRepository settings,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<UpdatePlatformSecuritySettingsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformSecuritySettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.UsersChangeAuthority, cancellationToken);

        if (!await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken))
        {
            return UpdatePlatformSecuritySettingsResponse.Failure(PlatformCommonErrors.StepUpRequired);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        PlatformSecuritySettings? settings = await _settings.GetAsync(cancellationToken);

        if (settings is null)
        {
            settings = PlatformSecuritySettings.Default(utcNow);
            _settings.Add(settings);
        }

        bool before = settings.RequireMfa;

        if (before == request.RequireMfa)
        {
            return UpdatePlatformSecuritySettingsResponse.Success();
        }

        settings.ChangeRequireMfa(request.RequireMfa, context.PlatformUserId, utcNow);
        _audit.Record(
            context.PlatformUserId,
            "platform.security.require_mfa_changed",
            PlatformPermissions.UsersChangeAuthority,
            nameof(PlatformSecuritySettings),
            settings.Id,
            before: new { requireMfa = before },
            after: new { requireMfa = settings.RequireMfa });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? UpdatePlatformSecuritySettingsResponse.Success()
            : UpdatePlatformSecuritySettingsResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
