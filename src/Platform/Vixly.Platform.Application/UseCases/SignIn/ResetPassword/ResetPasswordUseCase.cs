using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SignIn.ResetPassword;

// =========================================================
// FILE: ResetPasswordUseCase.cs
//
// PURPOSE:
// A staff member sets a new password from a reset link.
//
// RESPONSIBILITIES:
// - Check the link and the password policy
// - Set the new password, end every open session
// - Email a security notice
//
// IMPORTANT NOTES:
// - The second factor is not touched: a reset link alone
//   never gives access
// =========================================================
public sealed class ResetPasswordUseCase
{
    private readonly IPlatformAccountTokenRepository _tokens;

    private readonly IPlatformUserRepository _users;

    private readonly IPlatformSessionRepository _sessions;

    private readonly IPlatformPasswordHasher _hasher;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformIdentityMailer _mailer;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ResetPasswordUseCase(
        IPlatformAccountTokenRepository tokens,
        IPlatformUserRepository users,
        IPlatformSessionRepository sessions,
        IPlatformPasswordHasher hasher,
        IPlatformSecretGenerator secrets,
        IPlatformIdentityMailer mailer,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ResetPasswordResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformAccountToken? token = string.IsNullOrWhiteSpace(request.Token)
            ? null
            : await _tokens.FindByTokenHashAsync(_secrets.HashToken(request.Token), cancellationToken);
        PlatformUser? user = token is null ? null : await _users.GetAsync(token.PlatformUserId, cancellationToken);

        if (token is null || user is null)
        {
            return ResetPasswordResponse.Failure(PlatformAuthErrors.LinkInvalid);
        }

        Error? weak = PlatformPasswordPolicy.Check(request.NewPassword, user.Email.Value);

        if (weak is not null)
        {
            return ResetPasswordResponse.Failure(weak);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            token.Consume(PlatformAccountTokenPurpose.PasswordReset, utcNow);
            user.ChangePasswordHash(_hasher.Hash(request.NewPassword), utcNow);
        }
        catch (DomainException)
        {
            return ResetPasswordResponse.Failure(PlatformAuthErrors.LinkInvalid);
        }

        foreach (PlatformUserSession session in await _sessions.ListOpenAsync(user.Id, cancellationToken))
        {
            session.End(PlatformSessionEndReason.PasswordChanged, utcNow);
        }

        _audit.Record(user.Id, "platform.auth.password_reset", null, nameof(PlatformUser), user.Id);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ResetPasswordResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        await _mailer.SendSecurityNoticeAsync(user, "platform.notice.password_changed", cancellationToken);

        return ResetPasswordResponse.Success();
    }
}
