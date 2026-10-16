using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformLegalDocumentItem.cs
//
// PURPOSE:
// A legal document version as the console lists it.
//
// RESPONSIBILITIES:
// - Carry kind, label, status, dates and who published
//
// IMPORTANT NOTES:
// - Read model
// =========================================================
public sealed record PlatformLegalDocumentItem(
    Guid DocumentId,
    PlatformLegalDocumentKind Kind,
    string Version,
    PlatformLegalDocumentStatus Status,
    bool AcceptanceRequired,
    DateTimeOffset? EffectiveUtc,
    DateTimeOffset? DeadlineUtc,
    DateTimeOffset? PublishedUtc,
    string? PublishedByName,
    DateTimeOffset CreatedUtc);
