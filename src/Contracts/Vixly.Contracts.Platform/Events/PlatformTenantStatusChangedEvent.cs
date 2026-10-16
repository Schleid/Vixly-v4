namespace Vixly.Contracts.Platform.Events;

// =========================================================
// FILE: PlatformTenantStatusChangedEvent.cs
//
// PURPOSE:
// A tenant moved to another status.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Statuses: draft, pending_activation, active, suspended, deactivated, banned
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformTenantStatusChangedEvent(Guid TenantId, string TenantCode, string? FromStatus, string ToStatus, string? Category, DateTimeOffset OccurredUtc);
