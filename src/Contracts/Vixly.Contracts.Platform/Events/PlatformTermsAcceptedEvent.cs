namespace Vixly.Contracts.Platform.Events;

// =========================================================
// FILE: PlatformTermsAcceptedEvent.cs
//
// PURPOSE:
// A tenant accepted the Terms.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Sources: operations, registration, recorded_by_staff
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformTermsAcceptedEvent(Guid TenantId, Guid DocumentId, string Version, string Source, DateTimeOffset OccurredUtc);
