using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.ChangeMyPassword;

// =========================================================
// FILE: ChangeMyPasswordUseCase.cs
//
// PURPOSE:
// The signed-in user changes their own password.
//
// RESPONSIBILITIES:
// - Check the current password; a wrong one counts like a wrong
//   step-up code (the fifth ends the session)
// - Apply the password policy (length, common list, no email)
// - Save the new hash, end every other session, audit, commit,
//   then send a security notice
//
// IMPORTANT NOTES:
// - The session in use stays open, so the user is not thrown out
// - Nobody else can set a password (D-S1)
// =========================================================
public sealed class ChangeMyPasswordUseCase
{
    private const string NoticeCode = "platform.notice.password_changed";

    private readonly PlatformStepUpSteps _steps;

    private readonly IPlatformPasswordHasher _hasher;

    private readonly IPlatformSessionRepository _sessions;

    private readonly IPlatformIdentityMailer _mailer;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ChangeMyPasswordUseCase(
        PlatformStepUpSteps steps,
        IPlatformPasswordHasher hasher,
        IPlatformSessionRepository sessions,
        IPlatformIdentityMailer mailer,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ChangeMyPasswordResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ChangeMyPasswordRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _steps.LoadAsync(context, cancellationToken) is not { } found)
        {
            return ChangeMyPasswordResponse.Failure(PlatformAuthErrors.SessionEnded);
        }

        string? currentHash = found.User.Account.PasswordHash;

        if (currentHash is null
            || string.IsNullOrEmpty(request.CurrentPassword)
            || _hasher.Verify(request.CurrentPassword, currentHash) == PlatformPasswordCheck.Failed)
        {
            Error failed = await _steps.FailAsync(found.Session, found.User, "password", PlatformAuthErrors.PasswordIncorrect, cancellationToken);
            return ChangeMyPasswordResponse.Failure(failed);
        }

        Error? weak = PlatformPasswordPolicy.Check(request.NewPassword, found.User.Email.Value);

        if (weak is not null)
        {
            return ChangeMyPasswordResponse.Failure(weak);
        }

        DateTimeOffset utcNow = _steps.UtcNow;
        found.User.ChangePasswordHash(_hasher.Hash(request.NewPassword), utcNow);
        int ended = 0;

        foreach (PlatformUserSession session in await _sessions.ListOpenAsync(found.User.Id, cancellationToken))
        {
            if (session.Id != found.Session.Id)
            {
                session.End(PlatformSessionEndReason.PasswordChanged, utcNow);
                ended++;
            }
        }

        _audit.Record(found.User.Id, "platform.account.password_changed", null, nameof(PlatformUser), found.User.Id, arguments: new { otherSessionsEnded = ended });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ChangeMyPasswordResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        await _mailer.SendSecurityNoticeAsync(found.User, NoticeCode, cancellationToken);

        return ChangeMyPasswordResponse.Success(ended);
    }
}
