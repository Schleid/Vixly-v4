namespace Vixly.Platform.Application.Maintenance;

// =========================================================
// FILE: PlatformEdgeWindow.cs
//
// PURPOSE:
// One maintenance window as the Cloudflare Worker receives it.
//
// RESPONSIBILITIES:
// - Carry the times, the site codes, the messages and the hash of the pass link
//
// IMPORTANT NOTES:
// - Never the pass link itself: only its SHA-256 hash, the same as Platform keeps
// - Messages by language code (en, fr, es); a missing language is left out
// =========================================================
public sealed record PlatformEdgeWindow(
    Guid Id,
    DateTimeOffset StartsUtc,
    DateTimeOffset EndsUtc,
    IReadOnlyList<string> Sites,
    IReadOnlyDictionary<string, string> Messages,
    string PassHash);
