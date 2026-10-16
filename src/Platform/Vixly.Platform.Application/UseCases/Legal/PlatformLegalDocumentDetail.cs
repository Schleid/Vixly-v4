using Vixly.Platform.Application.Queries;

namespace Vixly.Platform.Application.UseCases.Legal;

// =========================================================
// FILE: PlatformLegalDocumentDetail.cs
//
// PURPOSE:
// A legal document version with its files, included versions and what the reader may do.
//
// RESPONSIBILITIES:
// - Carry the document page in one read
//
// IMPORTANT NOTES:
// - Options are the published versions a Terms draft can include
// =========================================================
public sealed record PlatformLegalDocumentDetail(
    PlatformLegalDocumentItem Document,
    IReadOnlyList<PlatformLegalFileItem> Files,
    IReadOnlyList<PlatformLegalDocumentItem> Parts,
    IReadOnlyList<PlatformLegalDocumentItem> PartOptions,
    PlatformLegalAbilities Abilities);
