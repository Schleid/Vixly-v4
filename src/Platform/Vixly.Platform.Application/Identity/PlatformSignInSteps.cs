using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Identity;

// =========================================================
// FILE: PlatformSignInSteps.cs
//
// PURPOSE:
// The steps every sign-in path shares: load a pending
// challenge, issue the next challenge, and finish by opening
// a session.
//
// RESPONSIBILITIES:
// - Find a challenge by its raw token and check its stage
// - Issue a challenge for a user (second factor or enrolment)
// - Complete a challenge: record the sign-in on the user,
//   open a session, audit, and commit in one SaveChanges
// - Open a session directly after a passkey sign-in, which
//   needs no challenge
//
// IMPORTANT NOTES:
// - Used only by sign-in use cases; holds no state
// - A finished sign-in sets the session's second-factor time,
//   so step-up is satisfied for the first 5 minutes
// =========================================================
public sealed class PlatformSignInSteps
{
    private readonly IPlatformSignInChallengeRepository _challenges;

    private readonly IPlatformUserRepository _users;

    private readonly IPlatformSessionRepository _sessions;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformRequestMetadata _metadata;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PlatformSignInSteps(
        IPlatformSignInChallengeRepository challenges,
        IPlatformUserRepository users,
        IPlatformSessionRepository sessions,
        IPlatformSecretGenerator secrets,
        IPlatformRequestMetadata metadata,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _challenges = challenges ?? throw new ArgumentNullException(nameof(challenges));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public string IssueChallenge(PlatformUser user, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(user);

        PlatformSecret secret = _secrets.NewToken();
        PlatformSignInStage stage = user.HasSecondFactor ? PlatformSignInStage.SecondFactor : PlatformSignInStage.Enrolment;
        _challenges.Add(PlatformSignInChallenge.Issue(user.Id, secret.Hash, stage, utcNow));

        return secret.Value;
    }

    public async Task<(PlatformSignInChallenge Challenge, PlatformUser User)?> LoadAsync(
        string? challengeToken,
        PlatformSignInStage stage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(challengeToken))
        {
            return null;
        }

        PlatformSignInChallenge? challenge =
            await _challenges.FindByTokenHashAsync(_secrets.HashToken(challengeToken), cancellationToken);

        if (challenge is null || challenge.Stage != stage || !challenge.IsUsableAt(UtcNow))
        {
            return null;
        }

        PlatformUser? user = await _users.GetAsync(challenge.PlatformUserId, cancellationToken);

        if (user is null || !user.Account.IsSignInAllowed(UtcNow))
        {
            return null;
        }

        return (challenge, user);
    }

    public async Task RecordFailedAttemptAsync(
        PlatformSignInChallenge challenge,
        PlatformUser user,
        string actionCode,
        CancellationToken cancellationToken)
    {
        challenge.RecordFailedAttempt(UtcNow);
        _audit.Record(null, actionCode, null, nameof(PlatformUser), user.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PlatformIssuedSession?> CompleteAsync(
        PlatformSignInChallenge challenge,
        PlatformUser user,
        string method,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(challenge);

        challenge.Complete(UtcNow);

        return await StartSessionAsync(user, method, cancellationToken);
    }

    public async Task<PlatformIssuedSession?> StartSessionAsync(
        PlatformUser user,
        string method,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        DateTimeOffset utcNow = UtcNow;
        PlatformSecret token = _secrets.NewToken();
        PlatformUserSession session = PlatformUserSession.Start(
            user.Id,
            token.Hash,
            _metadata.ClientIp,
            _metadata.UserAgent,
            utcNow);
        session.From(_metadata.Country);

        user.RecordSuccessfulSignIn(utcNow);
        _sessions.Add(session);
        _audit.Record(user.Id, "platform.auth.signed_in", null, nameof(PlatformUser), user.Id, arguments: new { method, country = session.Country });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? new PlatformIssuedSession(session.Id, token.Value, session.ExpiresUtc)
            : null;
    }
}
