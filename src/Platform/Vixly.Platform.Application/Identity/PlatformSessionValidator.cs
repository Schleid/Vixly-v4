using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Identity;

// =========================================================
// FILE: PlatformSessionValidator.cs
//
// PURPOSE:
// Turns the session cookie of a request into a trusted
// authorization context, or nothing.
//
// RESPONSIBILITIES:
// - Find the session by the hash of the cookie token
// - End it when idle or past its absolute lifetime
// - Refuse it when the account is no longer active
// - Keep it alive (at most one write a minute)
// - Build the context from the database, never the cookie
// - Answer whether a session is still valid, for the Blazor
//   circuit's periodic check (no touch)
//
// IMPORTANT NOTES:
// - Called by the host on every request and on every Blazor
//   circuit check (S-2)
// - Returns null for anything wrong; the host then signs the
//   user out without saying why
// =========================================================
public sealed class PlatformSessionValidator
{
    private readonly IPlatformSessionRepository _sessions;

    private readonly IPlatformUserRepository _users;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PlatformSessionValidator(
        IPlatformSessionRepository sessions,
        IPlatformUserRepository users,
        IPlatformSecretGenerator secrets,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PlatformAuthorizationContext?> ValidateAsync(
        string? sessionToken,
        CancellationToken cancellationToken)
    {
        return (await ValidateWithUserAsync(sessionToken, cancellationToken))?.Context;
    }

    public async Task<PlatformValidatedSession?> ValidateWithUserAsync(
        string? sessionToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        PlatformUserSession? session =
            await _sessions.FindByTokenHashAsync(_secrets.HashToken(sessionToken), cancellationToken);

        if (session is null || session.IsEnded)
        {
            return null;
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        if (session.ExpiryReasonAt(utcNow) is PlatformSessionEndReason expired)
        {
            session.End(expired, utcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        PlatformUser? user = await _users.GetAsync(session.PlatformUserId, cancellationToken);

        if (user is null || !user.Account.IsActive)
        {
            session.End(PlatformSessionEndReason.AccountRestricted, utcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (session.Touch(utcNow))
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new PlatformValidatedSession(
            new PlatformAuthorizationContext(user.Id, user.AuthorityLevel, session.Id),
            $"{user.FirstName} {user.LastName}",
            user.PreferredLanguage.Value);
    }

    public async Task<bool> IsStillValidAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        PlatformUserSession? session = await _sessions.GetAsync(sessionId, cancellationToken);

        if (session is null || !session.IsValidAt(_timeProvider.GetUtcNow()))
        {
            return false;
        }

        PlatformUser? user = await _users.GetAsync(session.PlatformUserId, cancellationToken);

        return user is not null && user.Account.IsActive;
    }
}
