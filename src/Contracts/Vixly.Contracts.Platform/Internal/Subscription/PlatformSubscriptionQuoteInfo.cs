namespace Vixly.Contracts.Platform.Internal.Subscription;

// =========================================================
// FILE: PlatformSubscriptionQuoteInfo.cs
//
// PURPOSE:
// What a change costs and when it applies (call 21).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Timing: now or at_period_end
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformSubscriptionQuoteInfo(string Timing, string PlanCode, string Term, int Licences, decimal ChargeNow, decimal NextPeriodAmount, decimal NextPeriodNetAmount, string CurrencyCode, DateTimeOffset EffectiveUtc, bool EndsTrial);
