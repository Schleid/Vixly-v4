using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformStepUpGuard.cs
//
// PURPOSE:
// Default step-up check against the caller's session record.
//
// RESPONSIBILITIES:
// - Load the session named by the context
// - Require it to belong to the caller, be valid now, and
//   hold a second-factor confirmation within 5 minutes
//
// IMPORTANT NOTES:
// - Read only; confirming a second factor is its own use case
// =========================================================
public sealed class PlatformStepUpGuard : IPlatformStepUpGuard
{
    private readonly IPlatformSessionRepository _sessions;

    private readonly TimeProvider _timeProvider;

    private readonly Vixly.Platform.Application.Ports.IPlatformIdleTimesReader? _idle;

    public PlatformStepUpGuard(IPlatformSessionRepository sessions, TimeProvider timeProvider, Vixly.Platform.Application.Ports.IPlatformIdleTimesReader? idle = null)
    {
        _idle = idle;
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<bool> HasRecentSecondFactorAsync(
        PlatformAuthorizationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.SessionId is not Guid sessionId)
        {
            return false;
        }

        PlatformUserSession? session = await _sessions.GetAsync(sessionId, cancellationToken);
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        TimeSpan idle = _idle is null ? PlatformIdentityLimits.SessionIdleTimeout : (await _idle.GetAsync(cancellationToken)).Idle;

        return session is not null
            && session.PlatformUserId == context.PlatformUserId
            && session.IsValidAt(utcNow, idle)
            && session.HasRecentSecondFactor(utcNow);
    }
}
