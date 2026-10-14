using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.UseCases.Users.InvitePlatformUser;

// =========================================================
// FILE: InvitePlatformUserUseCase.cs
//
// PURPOSE:
// An administrator invites a new staff member to the Platform console.
//
// RESPONSIBILITIES:
// - Require platform.users.create; inviting with an authority
//   above Standard also needs platform.users.change_authority
//   (SuperUsers only)
// - Create the user (Invited) and a 72-hour invitation link
// - Audit, commit, then send the invitation email
//
// IMPORTANT NOTES:
// - Nobody chooses a password for someone else (D-S1)
// =========================================================
public sealed class InvitePlatformUserUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformAuthorizationService _authorization;

    private readonly IPlatformUserRepository _users;

    private readonly IPlatformAccountTokenRepository _tokens;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformIdentityMailer _mailer;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public InvitePlatformUserUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformAuthorizationService authorization,
        IPlatformUserRepository users,
        IPlatformAccountTokenRepository tokens,
        IPlatformSecretGenerator secrets,
        IPlatformIdentityMailer mailer,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<InvitePlatformUserResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        InvitePlatformUserRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.UsersCreate, cancellationToken);

        if (request.AuthorityLevel != PlatformUserAuthorityLevel.Standard
            && !await _authorization.IsAuthorizedAsync(context, PlatformPermissions.UsersChangeAuthority, cancellationToken))
        {
            return InvitePlatformUserResponse.Failure(PlatformAuthErrors.AuthorityNotAllowed);
        }

        PlatformUser user;

        try
        {
            EmailAddress email = EmailAddress.Create(request.Email);

            if (await _users.EmailExistsAsync(email, cancellationToken))
            {
                return InvitePlatformUserResponse.Failure(PlatformCommonErrors.EmailAlreadyUsed);
            }

            user = PlatformUser.Invite(
                request.FirstName,
                request.LastName,
                email,
                LanguageCode.Create(request.PreferredLanguage),
                request.AuthorityLevel,
                request.EmployeeNumber,
                string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : PhoneNumber.Create(request.PhoneNumber),
                _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return InvitePlatformUserResponse.Failure(exception.Error);
        }

        PlatformSecret token = _secrets.NewToken();
        _users.Add(user);
        _tokens.Add(PlatformAccountToken.Issue(user.Id, PlatformAccountTokenPurpose.Invitation, token.Hash, user.CreatedUtc));
        _audit.Record(
            context.PlatformUserId,
            "platform.users.invited",
            PlatformPermissions.UsersCreate,
            nameof(PlatformUser),
            user.Id,
            after: new { email = user.Email.Value, authority = user.AuthorityLevel.ToString(), language = user.PreferredLanguage.Value });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return InvitePlatformUserResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        await _mailer.SendInvitationAsync(user, token.Value, cancellationToken);

        return InvitePlatformUserResponse.Success(user.Id);
    }
}
