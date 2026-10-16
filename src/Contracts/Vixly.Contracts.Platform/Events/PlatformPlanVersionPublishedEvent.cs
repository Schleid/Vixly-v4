namespace Vixly.Contracts.Platform.Events;

// =========================================================
// FILE: PlatformPlanVersionPublishedEvent.cs
//
// PURPOSE:
// A new version of a plan is on sale.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - No tenant: drop cached plan lists
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformPlanVersionPublishedEvent(Guid PlanId, string PlanCode, Guid VersionId, int VersionNumber, DateTimeOffset OccurredUtc);
