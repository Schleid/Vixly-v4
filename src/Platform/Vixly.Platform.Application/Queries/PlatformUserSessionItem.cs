namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformUserSessionItem.cs
//
// PURPOSE:
// One open session of a staff user.
//
// RESPONSIBILITIES:
// - Carry when and from where the session started
//
// IMPORTANT NOTES:
// - Never carries the token or its hash
// =========================================================
public sealed record PlatformUserSessionItem(
    Guid SessionId,
    DateTimeOffset StartedUtc,
    DateTimeOffset LastSeenUtc,
    string? IpAddress,
    string? UserAgent);
