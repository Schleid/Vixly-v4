namespace Vixly.Contracts.Platform.Events;

// =========================================================
// FILE: PlatformSubscriptionStatusChangedEvent.cs
//
// PURPOSE:
// A subscription started, changed, renewed, ended or moved to another status.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Drop cached entitlements and portal access for the tenant
// - Change is the history code, for example platform.subscription_history.activated
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformSubscriptionStatusChangedEvent(Guid TenantId, Guid SubscriptionId, string Status, string Change, Guid PlanId, string Term, int Licences, DateTimeOffset? PeriodEndUtc, DateTimeOffset OccurredUtc);
