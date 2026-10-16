namespace Vixly.Contracts.Platform.Internal.Subscription;

// =========================================================
// FILE: PlatformScheduledChangeInfo.cs
//
// PURPOSE:
// A change that takes effect later.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformScheduledChangeInfo(string PlanCode, string PlanName, string Term, int Licences, DateTimeOffset EffectiveUtc, decimal PeriodAmount);
