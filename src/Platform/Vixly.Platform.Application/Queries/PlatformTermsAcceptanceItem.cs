using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTermsAcceptanceItem.cs
//
// PURPOSE:
// One acceptance with its evidence, for the tenant page.
//
// RESPONSIBILITIES:
// - Carry version, language, source, who, when, IP, user agent and hash
//
// IMPORTANT NOTES:
// - Evidence bytes are not carried
// =========================================================
public sealed record PlatformTermsAcceptanceItem(
    Guid AcceptanceId,
    string Version,
    string Language,
    PlatformTermsAcceptanceSource Source,
    DateTimeOffset AcceptedUtc,
    string? ActorName,
    string? ActorRole,
    string? ClientIp,
    string? UserAgent,
    string Sha256,
    string? RecordedByName,
    string? Reason,
    bool HasEvidence);
