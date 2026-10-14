using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.Users.SendPlatformUserPasswordReset;

// =========================================================
// FILE: SendPlatformUserPasswordResetUseCase.cs
//
// PURPOSE:
// An administrator sends an active staff user a password reset
// link (60 minutes). The administrator never sees or sets the
// password (D-S1).
//
// RESPONSIBILITIES:
// - Require platform.users.send_password_reset
// - Refuse unless the account is Active
// - Revoke earlier unused links of the same kind, issue a new
//   one, audit, commit, then send the email
//
// IMPORTANT NOTES:
// - The user's current password keeps working until the link
//   is used
// =========================================================
public sealed class SendPlatformUserPasswordResetUseCase
{
    private readonly PlatformUserAdministration _administration;

    private readonly IPlatformAccountTokenRepository _tokens;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformIdentityMailer _mailer;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SendPlatformUserPasswordResetUseCase(
        PlatformUserAdministration administration,
        IPlatformAccountTokenRepository tokens,
        IPlatformSecretGenerator secrets,
        IPlatformIdentityMailer mailer,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SendPlatformUserPasswordResetResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SendPlatformUserPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformUserTarget target = await _administration.LoadTargetAsync(
            context,
            PlatformPermissions.UsersSendPasswordReset,
            request.PlatformUserId,
            requireStepUp: false,
            cancellationToken);

        if (target.User is not PlatformUser user)
        {
            return SendPlatformUserPasswordResetResponse.Failure(target.Error!);
        }

        if (user.Account.Status != PlatformUserAccountStatus.Active)
        {
            return SendPlatformUserPasswordResetResponse.Failure(PlatformIdentityErrors.AccountNotActive);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        foreach (PlatformAccountToken earlier in await _tokens.ListUnusedAsync(user.Id, PlatformAccountTokenPurpose.PasswordReset, cancellationToken))
        {
            earlier.Revoke(utcNow);
        }

        PlatformSecret token = _secrets.NewToken();
        _tokens.Add(PlatformAccountToken.Issue(user.Id, PlatformAccountTokenPurpose.PasswordReset, token.Hash, utcNow));
        _audit.Record(context.PlatformUserId, "platform.users.password_reset_sent", PlatformPermissions.UsersSendPasswordReset, nameof(PlatformUser), user.Id);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SendPlatformUserPasswordResetResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        await _mailer.SendPasswordResetAsync(user, token.Value, cancellationToken);

        return SendPlatformUserPasswordResetResponse.Success();
    }
}
