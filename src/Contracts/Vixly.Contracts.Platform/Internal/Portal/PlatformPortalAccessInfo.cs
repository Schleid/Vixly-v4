namespace Vixly.Contracts.Platform.Internal.Portal;

// =========================================================
// FILE: PlatformPortalAccessInfo.cs
//
// PURPOSE:
// Whether a portal address belongs to a working tenant, for Edge (call 1).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Access: full, read_only, payment_required, none; Legal: not_required, accepted, due, overdue
// - Redirect when CanonicalSlug differs from the address asked
// - Cache 60 seconds (D-K6)
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformPortalAccessInfo(bool Found, Guid? TenantId, string? Status, string Access, string? CanonicalSlug, string? DefaultLanguage, string Legal);
