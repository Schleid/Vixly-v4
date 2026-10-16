namespace Vixly.Contracts.Platform.Internal.Entitlements;

// =========================================================
// FILE: PlatformEntitlementsBatchBody.cs
//
// PURPOSE:
// Up to 100 tenants for a background job.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformEntitlementsBatchBody(IReadOnlyList<Guid> TenantIds);
