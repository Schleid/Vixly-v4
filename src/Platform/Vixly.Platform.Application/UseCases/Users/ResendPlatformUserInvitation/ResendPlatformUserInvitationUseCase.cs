using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.Users.ResendPlatformUserInvitation;

// =========================================================
// FILE: ResendPlatformUserInvitationUseCase.cs
//
// PURPOSE:
// An administrator sends a staff user a fresh invitation link
// (72 hours) when the first one expired or was lost.
//
// RESPONSIBILITIES:
// - Require platform.users.invite
// - Refuse unless the account is Invited
// - Revoke earlier unused links of the same kind, issue a new
//   one, audit, commit, then send the email
//
// IMPORTANT NOTES:
// - Only one invitation link works at a time
// =========================================================
public sealed class ResendPlatformUserInvitationUseCase
{
    private readonly PlatformUserAdministration _administration;

    private readonly IPlatformAccountTokenRepository _tokens;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformIdentityMailer _mailer;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ResendPlatformUserInvitationUseCase(
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

    public async Task<ResendPlatformUserInvitationResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ResendPlatformUserInvitationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformUserTarget target = await _administration.LoadTargetAsync(
            context,
            PlatformPermissions.UsersInvite,
            request.PlatformUserId,
            requireStepUp: false,
            cancellationToken);

        if (target.User is not PlatformUser user)
        {
            return ResendPlatformUserInvitationResponse.Failure(target.Error!);
        }

        if (user.Account.Status != PlatformUserAccountStatus.Invited)
        {
            return ResendPlatformUserInvitationResponse.Failure(PlatformAuthErrors.AccountNotInvited);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        foreach (PlatformAccountToken earlier in await _tokens.ListUnusedAsync(user.Id, PlatformAccountTokenPurpose.Invitation, cancellationToken))
        {
            earlier.Revoke(utcNow);
        }

        PlatformSecret token = _secrets.NewToken();
        _tokens.Add(PlatformAccountToken.Issue(user.Id, PlatformAccountTokenPurpose.Invitation, token.Hash, utcNow));
        _audit.Record(context.PlatformUserId, "platform.users.invitation_resent", PlatformPermissions.UsersInvite, nameof(PlatformUser), user.Id);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ResendPlatformUserInvitationResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        await _mailer.SendInvitationAsync(user, token.Value, cancellationToken);

        return ResendPlatformUserInvitationResponse.Success();
    }
}
