namespace Vixly.Contracts.Platform.Internal.Subscription;

// =========================================================
// FILE: PlatformPlanOptionInfo.cs
//
// PURPOSE:
// A plan the tenant may move to (call 20).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformPlanOptionInfo(Guid PlanId, string Code, string Name, string CurrencyCode, decimal MonthlyPricePerLicence, decimal AnnualPricePerLicence, decimal SetupFee, int MinimumLicences, int MaxLicences, int? TrialDays);
