namespace Vixly.Platform.Application.UseCases.Legal;

// =========================================================
// FILE: PlatformLegalTerms.cs
//
// PURPOSE:
// The current Terms in one language with what it includes: what the registration page and the owner accept.
//
// RESPONSIBILITIES:
// - Carry the version, dates, hash, summary and parts
//
// IMPORTANT NOTES:
// - Accepting sends back the document id and hash shown, so a newer version in between is refused
// =========================================================
public sealed record PlatformLegalTerms(
    Guid DocumentId,
    string Version,
    string Language,
    string Sha256,
    string? Summary,
    DateTimeOffset? EffectiveUtc,
    IReadOnlyList<PlatformLegalPart> Parts);
