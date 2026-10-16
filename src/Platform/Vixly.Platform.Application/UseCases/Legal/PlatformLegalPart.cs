using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.UseCases.Legal;

// =========================================================
// FILE: PlatformLegalPart.cs
//
// PURPOSE:
// One document a Terms version includes, as the public page shows it.
//
// RESPONSIBILITIES:
// - Carry kind, version, hash and summary in one language
//
// IMPORTANT NOTES:
// - Files are downloaded by id and language
// =========================================================
public sealed record PlatformLegalPart(Guid DocumentId, PlatformLegalDocumentKind Kind, string Version, string Sha256, string? Summary);
