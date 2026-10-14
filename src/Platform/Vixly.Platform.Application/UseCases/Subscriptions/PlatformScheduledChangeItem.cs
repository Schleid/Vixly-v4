using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Subscriptions;

// =========================================================
// FILE: PlatformScheduledChangeItem.cs
//
// PURPOSE:
// A change waiting for the period end.
//
// RESPONSIBILITIES:
// - Carry the target plan, term, licences, date and next period amount
//
// IMPORTANT NOTES:
// - Amount before tax
// =========================================================
public sealed record PlatformScheduledChangeItem(
    string PlanCode,
    string PlanName,
    PlatformSubscriptionTerm Term,
    int Licences,
    DateTimeOffset EffectiveUtc,
    decimal PeriodAmount);
