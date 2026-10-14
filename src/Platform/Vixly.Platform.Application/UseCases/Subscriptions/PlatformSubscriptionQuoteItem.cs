using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionQuoteItem.cs
//
// PURPOSE:
// A previewed change, as the console shows it.
//
// RESPONSIBILITIES:
// - Carry timing, target, charge now, next period amount, currency and date
//
// IMPORTANT NOTES:
// - Amounts before tax
// =========================================================
public sealed record PlatformSubscriptionQuoteItem(
    PlatformSubscriptionChangeTiming Timing,
    string PlanCode,
    PlatformSubscriptionTerm Term,
    int Licences,
    decimal ChargeNow,
    decimal NextPeriodAmount,
    string CurrencyCode,
    DateTimeOffset EffectiveUtc,
    bool EndsTrial,
    decimal NextPeriodNetAmount)
{
    public static PlatformSubscriptionQuoteItem From(PlatformSubscriptionQuote quote)
    {
        ArgumentNullException.ThrowIfNull(quote);

        return new PlatformSubscriptionQuoteItem(
            quote.Timing,
            quote.TargetPlanCode,
            quote.TargetTerm,
            quote.TargetLicences,
            quote.ChargeNow,
            quote.NextPeriodAmount,
            quote.CurrencyCode,
            quote.EffectiveUtc,
            quote.EndsTrial,
            quote.NextPeriodNetAmount);
    }
}
