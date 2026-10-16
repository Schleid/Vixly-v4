namespace Vixly.Contracts.Platform.Internal.Entitlements;

// =========================================================
// FILE: PlatformEntitlementsLegalInfo.cs
//
// PURPOSE:
// The Terms the owner must accept.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Status: due or overdue; overdue makes the tenant read-only
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformEntitlementsLegalInfo(string Status, DateTimeOffset? DeadlineUtc, string? RequiredVersion);
