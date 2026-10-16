using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformLegalVersionPublished.cs
//
// PURPOSE:
// Event for Operations: a legal document version was published (D-Lg11).
//
// RESPONSIBILITIES:
// - Carry the document, kind, version and dates
//
// IMPORTANT NOTES:
// - Version 1
// =========================================================
public sealed record PlatformLegalVersionPublished(Guid DocumentId, PlatformLegalDocumentKind Kind, string Version, bool AcceptanceRequired, DateTimeOffset? EffectiveUtc, DateTimeOffset? DeadlineUtc, DateTimeOffset OccurredUtc);
