namespace Vixly.Contracts.Platform.Legal;

// =========================================================
// FILE: PlatformLegalTermsInfo.cs
//
// PURPOSE:
// The current Terms in one language, as the registration page shows it (Slice 8).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Start a registration with Version and the Privacy part's version
// - FileUrl values are relative to the Platform.Api base address
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformLegalTermsInfo(
    Guid DocumentId,
    string Version,
    string Language,
    string Sha256,
    string? Summary,
    DateTimeOffset? EffectiveUtc,
    string FileUrl,
    IReadOnlyList<PlatformLegalPartInfo> Parts);
