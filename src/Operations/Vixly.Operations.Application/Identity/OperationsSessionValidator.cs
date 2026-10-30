using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsSessionValidator.cs
//
// PURPOSE:
// Checks a staff session cookie on each request (Slice 1, D-O6).
//
// RESPONSIBILITIES:
// - Find the session of the current tenant
// - End it after 15 idle minutes or 12 hours, or when the account is no longer active
// - Write last seen at most once a minute
//
// IMPORTANT NOTES:
// - A cookie from another tenant host finds nothing (D-F1)
// =========================================================
public sealed class OperationsSessionValidator
{
    private readonly IOperationsSessionRepository _sessions;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public OperationsSessionValidator(
        IOperationsSessionRepository sessions,
        IOperationsUserRepository users,
        IOperationsSecretGenerator secrets,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<OperationsValidatedSession?> ValidateAsync(string? sessionToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        OperationsUserSession? session = await _sessions.FindByTokenHashAsync(_secrets.HashToken(sessionToken), cancellationToken);

        if (session is null || session.IsEnded)
        {
            return null;
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        if (session.ExpiryReasonAt(utcNow) is OperationsSessionEndReason expired)
        {
            session.End(expired, utcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        OperationsUser? user = await _users.GetAsync(session.UserId, cancellationToken);

        if (user is null || !user.Account.IsActive)
        {
            session.End(OperationsSessionEndReason.AccountRestricted, utcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (session.Touch(utcNow))
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new OperationsValidatedSession(user.Id, session.Id, user.FullName, user.PreferredLanguage.Value, user.IsOwner, session.IsSetupSession, user.IsAdministrator);
    }
}
