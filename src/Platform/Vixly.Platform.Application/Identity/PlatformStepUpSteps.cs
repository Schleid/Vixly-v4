using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.Identity;

// =========================================================
// FILE: PlatformStepUpSteps.cs
//
// PURPOSE:
// The steps every step-up path shares (ADR-0006 section 5):
// authenticator code, recovery code or passkey.
//
// RESPONSIBILITIES:
// - Load the signed-in user's open session and active account
// - Confirm: open the 5-minute window, audit, commit
// - Fail: count it on the session (the fifth ends it), audit,
//   commit
//
// IMPORTANT NOTES:
// - Used only by step-up use cases; holds no state
// =========================================================
public sealed class PlatformStepUpSteps
{
    private readonly IPlatformSessionRepository _sessions;

    private readonly IPlatformUserRepository _users;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PlatformStepUpSteps(
        IPlatformSessionRepository sessions,
        IPlatformUserRepository users,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public async Task<(PlatformUserSession Session, PlatformUser User)?> LoadAsync(
        PlatformAuthorizationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        PlatformUserSession? session = context.SessionId is Guid sessionId
            ? await _sessions.GetAsync(sessionId, cancellationToken)
            : null;

        if (session is null || session.PlatformUserId != context.PlatformUserId || !session.IsValidAt(UtcNow))
        {
            return null;
        }

        PlatformUser? user = await _users.GetAsync(context.PlatformUserId, cancellationToken);

        return user is null || !user.Account.IsActive ? null : (session, user);
    }

    public async Task<Error?> ConfirmAsync(
        PlatformUserSession session,
        PlatformUser user,
        string method,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(user);

        session.ConfirmSecondFactor(UtcNow);
        _audit.Record(user.Id, "platform.auth.step_up_confirmed", null, nameof(PlatformUser), user.Id, arguments: new { method });

        return await _unitOfWork.SaveChangesAsync(cancellationToken) ? null : PlatformCommonErrors.ConcurrencyConflict;
    }

    public async Task<Error> FailAsync(
        PlatformUserSession session,
        PlatformUser user,
        string method,
        Error wrongAnswer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(user);

        bool ended = session.RecordFailedStepUp(UtcNow);
        _audit.Record(user.Id, "platform.auth.step_up_failed", null, nameof(PlatformUser), user.Id, arguments: new { method, ended });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ended ? PlatformAuthErrors.SessionEnded : wrongAnswer;
    }
}
