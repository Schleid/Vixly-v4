namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsSubscription.cs
//
// PURPOSE:
// The company's plan at Vixly as the billing page shows it.
//
// RESPONSIBILITIES:
// - Carry what Platform answered
//
// IMPORTANT NOTES:
// - Read-only; mapped from Contracts.Platform in Infrastructure
// =========================================================
public sealed record OperationsSubscription(string PlanName, string Status, string Term, int Licences, int ActiveLicences, decimal PeriodAmount, string CurrencyCode, DateTimeOffset? TrialEndsUtc, DateTimeOffset? PeriodEndUtc, bool CancelAtPeriodEnd);
