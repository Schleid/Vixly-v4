namespace Vixly.Platform.Application.UseCases.SecuritySettings.ChangePlatformIdleTimes;

// =========================================================
// FILE: ChangePlatformIdleTimesRequest.cs
//
// PURPOSE:
// Control's new inactivity sign-out and warning, in minutes (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Carry the values named above, nothing more
//
// IMPORTANT NOTES:
// - Checked by PlatformSecuritySettings.ChangeIdle
// =========================================================
public sealed class ChangePlatformIdleTimesRequest
{
    public int IdleMinutes { get; init; }

    public int IdleWarnMinutes { get; init; }
}
