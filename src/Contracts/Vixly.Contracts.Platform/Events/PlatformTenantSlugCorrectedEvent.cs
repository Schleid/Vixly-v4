namespace Vixly.Contracts.Platform.Events;

// =========================================================
// FILE: PlatformTenantSlugCorrectedEvent.cs
//
// PURPOSE:
// A tenant's portal address was corrected; the old one redirects.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformTenantSlugCorrectedEvent(Guid TenantId, string FromSlug, string ToSlug, DateTimeOffset OccurredUtc);
