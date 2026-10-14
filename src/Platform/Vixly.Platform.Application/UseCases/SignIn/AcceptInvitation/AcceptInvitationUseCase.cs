using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SignIn.AcceptInvitation;

// =========================================================
// FILE: AcceptInvitationUseCase.cs
//
// PURPOSE:
// A new staff member accepts their invitation and sets a password.
//
// RESPONSIBILITIES:
// - Check the link and the password policy
// - Consume the link, set the password, activate the account
// - Issue a pending sign-in for enrolment
//
// IMPORTANT NOTES:
// - Any link problem answers LinkInvalid, without detail
// =========================================================
public sealed class AcceptInvitationUseCase
{
    private readonly IPlatformAccountTokenRepository _tokens;

    private readonly IPlatformUserRepository _users;

    private readonly IPlatformPasswordHasher _hasher;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly PlatformSignInSteps _steps;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public AcceptInvitationUseCase(
        IPlatformAccountTokenRepository tokens,
        IPlatformUserRepository users,
        IPlatformPasswordHasher hasher,
        IPlatformSecretGenerator secrets,
        PlatformSignInSteps steps,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<AcceptInvitationResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        AcceptInvitationRequest request,
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
            return AcceptInvitationResponse.Failure(PlatformAuthErrors.LinkInvalid);
        }

        Error? weak = PlatformPasswordPolicy.Check(request.Password, user.Email.Value);

        if (weak is not null)
        {
            return AcceptInvitationResponse.Failure(weak);
        }

        DateTimeOffset utcNow = _steps.UtcNow;

        try
        {
            token.Consume(PlatformAccountTokenPurpose.Invitation, utcNow);
            user.AcceptInvitation(_hasher.Hash(request.Password), utcNow);
        }
        catch (DomainException)
        {
            return AcceptInvitationResponse.Failure(PlatformAuthErrors.LinkInvalid);
        }

        string challengeToken = _steps.IssueChallenge(user, utcNow);
        _audit.Record(user.Id, "platform.auth.invitation_accepted", null, nameof(PlatformUser), user.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? AcceptInvitationResponse.Success(challengeToken)
            : AcceptInvitationResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
