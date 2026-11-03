using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Identity;

// =========================================================
// FILE: PlatformIdleTimes.cs
//
// PURPOSE:
// Control's inactivity sign-out and the warning before it (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Carry the two times from the security settings, or the defaults
//
// IMPORTANT NOTES:
// - Before 2026-10-27: 5 minutes, the warning 1 minute before, fixed in the code
// =========================================================
public sealed record PlatformIdleTimes(TimeSpan Idle, TimeSpan Warn)
{
    public static PlatformIdleTimes Default { get; } = new(PlatformIdentityLimits.SessionIdleTimeout, TimeSpan.FromMinutes(PlatformIdentityLimits.IdleWarnMinutesDefault));
}
