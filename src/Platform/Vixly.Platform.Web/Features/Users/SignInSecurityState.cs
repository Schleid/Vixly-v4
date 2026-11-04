using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Users;

// =========================================================
// FILE: SignInSecurityState.cs
//
// PURPOSE:
// What the sign-in security panel shows (Require MFA; the inactivity sign-out, owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Carry the values named above, nothing more
//
// IMPORTANT NOTES:
// - Part of Require MFA (owner, 2026-10-22)
// =========================================================
public sealed record SignInSecurityState(
    bool Loaded,
    bool RequireMfa,
    string? UpdatedBy,
    DateTimeOffset? UpdatedUtc,
    bool CanEdit,
    bool? Pending,
    string? DoneKey,
    Error? Error,
    int IdleMinutes = 5,
    int IdleWarnMinutes = 1,
    bool CanEditIdle = false,
    (int Idle, int Warn)? PendingIdle = null)
{
    public static SignInSecurityState Initial { get; } = new(false, true, null, null, false, null, null, null);
}
