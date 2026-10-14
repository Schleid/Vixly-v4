namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionQuote.cs
//
// PURPOSE:
// What a requested change would do, before it is made.
//
// RESPONSIBILITIES:
// - Say whether a change applies now or at the period end
// - Give the prorated charge now and the next period's amount, in the plan's currency
//
// IMPORTANT NOTES:
// - Produced by PlatformSubscription.Quote; used for the preview and for the change itself, so both agree
// - Amounts are before tax (D-S9)
// - ChargeNow is after the current discount; NextPeriodAmount is the list amount and NextPeriodNetAmount what is billed
// =========================================================
public sealed record PlatformSubscriptionQuote(
    PlatformSubscriptionChangeTiming Timing,
    Guid TargetPlanId,
    string TargetPlanCode,
    Guid TargetVersionId,
    int TargetVersionNumber,
    PlatformSubscriptionTerm TargetTerm,
    int TargetLicences,
    decimal ChargeNow,
    decimal NextPeriodAmount,
    string CurrencyCode,
    DateTimeOffset EffectiveUtc,
    bool EndsTrial,
    decimal NextPeriodNetAmount);
