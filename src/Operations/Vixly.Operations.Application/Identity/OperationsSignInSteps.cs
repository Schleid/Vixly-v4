using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsSignInSteps.cs
//
// PURPOSE:
// The shared steps of staff sign-in (Slice 1).
//
// RESPONSIBILITIES:
// - Issue a pending sign-in after the password
// - Load it back for the second factor
// - Start the session
//
// IMPORTANT NOTES:
// - The tenant comes from the context: a token from another tenant host is never found (D-F1)
// - A session started while the tenant is being set up is a setup session
// =========================================================
public sealed class OperationsSignInSteps
{
    private readonly IOperationsSignInChallengeRepository _challenges;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsSessionRepository _sessions;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly IOperationsRequestMetadata _metadata;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly IOperationsSettingsRepository _settings;

    public OperationsSignInSteps(
        IOperationsSignInChallengeRepository challenges,
        IOperationsUserRepository users,
        IOperationsSessionRepository sessions,
        IOperationsSecretGenerator secrets,
        IOperationsRequestMetadata metadata,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IOperationsSettingsRepository settings)
    {
        _challenges = challenges ?? throw new ArgumentNullException(nameof(challenges));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    // The country the request comes from (Cloudflare's), when known.
    public string? Country => _metadata.Country;

    // The Owner and Administrators always need a second factor; other staff when the tenant requires it for all (D-O2).
    public async Task<bool> NeedsSecondFactorAsync(OperationsUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.RequiresSecondFactor || (await _settings.FindAsync(cancellationToken))?.RequireSecondFactorForAllStaff == true;
    }

    public string IssueChallenge(OperationsAuthorizationContext context, OperationsUser user, DateTimeOffset utcNow, OperationsSignInPlace? place = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(user);

        OperationsSecret secret = _secrets.NewToken();
        OperationsSignInStage stage = user.HasSecondFactor ? OperationsSignInStage.SecondFactor : OperationsSignInStage.Enrolment;
        OperationsSignInChallenge challenge = OperationsSignInChallenge.Issue(context.RequiredTenantId, user.Id, secret.Hash, stage, utcNow);
        challenge.At(place ?? OperationsSignInPlace.Nowhere(Country));
        _challenges.Add(challenge);

        return secret.Value;
    }

    public async Task<(OperationsSignInChallenge Challenge, OperationsUser User)?> LoadAsync(string? challengeToken, OperationsSignInStage stage, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(challengeToken))
        {
            return null;
        }

        OperationsSignInChallenge? challenge = await _challenges.FindByTokenHashAsync(_secrets.HashToken(challengeToken), cancellationToken);

        if (challenge is null || challenge.Stage != stage || !challenge.IsUsableAt(UtcNow))
        {
            return null;
        }

        OperationsUser? user = await _users.GetAsync(challenge.UserId, cancellationToken);

        return user is null || !user.Account.IsSignInAllowed(UtcNow) ? null : (challenge, user);
    }

    public async Task RecordFailedAttemptAsync(OperationsSignInChallenge challenge, OperationsUser user, string actionCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        ArgumentNullException.ThrowIfNull(user);

        challenge.RecordFailedAttempt(UtcNow);
        _audit.Record(null, actionCode, nameof(OperationsUser), user.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<OperationsIssuedSession?> CompleteAsync(OperationsAuthorizationContext context, OperationsSignInChallenge challenge, OperationsUser user, string method, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(challenge);

        challenge.Complete(UtcNow);
        return StartSessionAsync(context, user, method, cancellationToken, challenge.Place);
    }

    public async Task<OperationsIssuedSession?> StartSessionAsync(OperationsAuthorizationContext context, OperationsUser user, string method, CancellationToken cancellationToken,
        OperationsSignInPlace? place = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(user);

        DateTimeOffset utcNow = UtcNow;
        OperationsSecret token = _secrets.NewToken();
        OperationsUserSession session = OperationsUserSession.Start(context.RequiredTenantId, user.Id, token.Hash, _metadata.ClientIp, _metadata.UserAgent, context.IsSettingUp, utcNow);
        OperationsSignInPlace where = place ?? OperationsSignInPlace.Nowhere(Country);
        session.At(where);
        user.RecordSuccessfulSignIn(utcNow);
        _sessions.Add(session);
        _audit.Record(user.Id, "ops.auth.signed_in", nameof(OperationsUser), user.Id,
            arguments: new { method, setup = context.IsSettingUp, device = where.DeviceNumber, country = where.Country, countryChanged = where.CountryChanged });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? new OperationsIssuedSession(session.Id, token.Value, session.ExpiresUtc)
            : null;
    }
}
