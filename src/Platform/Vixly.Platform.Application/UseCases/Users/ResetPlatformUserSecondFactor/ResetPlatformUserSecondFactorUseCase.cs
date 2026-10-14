using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Users.ResetPlatformUserSecondFactor;

// =========================================================
// FILE: ResetPlatformUserSecondFactorUseCase.cs
//
// PURPOSE:
// A security administrator removes a staff user's passkeys,
// authenticator app and recovery codes, for a lost device.
//
// RESPONSIBILITIES:
// - Require platform.users.reset_second_factor and a recent
//   second factor
// - Remove every second factor, end every session, audit,
//   then send the user a security notice
//
// IMPORTANT NOTES:
// - At the next sign-in the user must enrol again before any
//   page loads (ADR-0006 section 3)
// - The password is unchanged
// =========================================================
public sealed class ResetPlatformUserSecondFactorUseCase
{
    private const string NoticeCode = "platform.notice.second_factor_reset";

    private readonly PlatformUserAdministration _administration;

    private readonly IPlatformSessionRepository _sessions;

    private readonly IPlatformIdentityMailer _mailer;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ResetPlatformUserSecondFactorUseCase(
        PlatformUserAdministration administration,
        IPlatformSessionRepository sessions,
        IPlatformIdentityMailer mailer,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ResetPlatformUserSecondFactorResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ResetPlatformUserSecondFactorRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformUserTarget target = await _administration.LoadTargetAsync(
            context,
            PlatformPermissions.UsersResetSecondFactor,
            request.PlatformUserId,
            requireStepUp: true,
            cancellationToken);

        if (target.User is not PlatformUser user)
        {
            return ResetPlatformUserSecondFactorResponse.Failure(target.Error!);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        var before = new { authenticatorApp = user.AuthenticatorApp?.IsConfirmed == true, passkeys = user.Passkeys.Count, recoveryCodes = user.UnusedRecoveryCodeCount };

        try
        {
            user.ResetSecondFactor(request.Reason, context.PlatformUserId, utcNow);
        }
        catch (DomainException exception)
        {
            return ResetPlatformUserSecondFactorResponse.Failure(exception.Error);
        }

        foreach (PlatformUserSession session in await _sessions.ListOpenAsync(user.Id, cancellationToken))
        {
            session.End(PlatformSessionEndReason.SecondFactorReset, utcNow);
        }

        _audit.Record(
            context.PlatformUserId,
            "platform.users.second_factor_reset",
            PlatformPermissions.UsersResetSecondFactor,
            nameof(PlatformUser),
            user.Id,
            before: before,
            reason: request.Reason.Trim());

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ResetPlatformUserSecondFactorResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        await _mailer.SendSecurityNoticeAsync(user, NoticeCode, cancellationToken);

        return ResetPlatformUserSecondFactorResponse.Success();
    }
}
