namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformSignInItem.cs
//
// PURPOSE:
// One sign-in to Control: who, when, from where, and how it ended (owner, 2026-10-21: member activity).
//
// RESPONSIBILITIES:
// - Carry the values the activity list shows
//
// IMPORTANT NOTES:
// - Ended: the end reason in snake case; none while the session may still be in use
// - A session nobody ended stops at its idle or absolute limit
// =========================================================
public sealed record PlatformSignInItem(
    Guid SessionId,
    Guid PlatformUserId,
    string UserName,
    DateTimeOffset SignedInUtc,
    DateTimeOffset? SignedOutUtc,
    string? Ended,
    string? IpAddress,
    string? UserAgent,
    string? Country);
