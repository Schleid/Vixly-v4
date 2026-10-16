namespace Vixly.Contracts.Platform.Events;

// =========================================================
// FILE: PlatformLegalVersionPublishedEvent.cs
//
// PURPOSE:
// A legal document version was published.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - No tenant: drop every cached legal status
// - Kinds: terms, privacy, data_processing, acceptable_use, service_level
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformLegalVersionPublishedEvent(Guid DocumentId, string Kind, string Version, bool AcceptanceRequired, DateTimeOffset? EffectiveUtc, DateTimeOffset? DeadlineUtc, DateTimeOffset OccurredUtc);
