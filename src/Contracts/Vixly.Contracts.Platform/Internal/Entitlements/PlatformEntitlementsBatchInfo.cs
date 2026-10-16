namespace Vixly.Contracts.Platform.Internal.Entitlements;

// =========================================================
// FILE: PlatformEntitlementsBatchInfo.cs
//
// PURPOSE:
// Entitlements for a batch of tenants.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformEntitlementsBatchInfo(IReadOnlyList<PlatformEntitlementsInfo> Items);
