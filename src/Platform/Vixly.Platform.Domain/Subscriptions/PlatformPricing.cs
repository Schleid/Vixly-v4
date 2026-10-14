namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformPricing.cs
//
// PURPOSE:
// The arithmetic of subscription prices (D-S9).
//
// RESPONSIBILITIES:
// - Price a period for a version, a term and a number of licences
// - Compute period ends and the share of a period still to run
// - Prorate upgrades and added licences the way V3 did
//
// IMPORTANT NOTES:
// - Amounts are before tax; Billing adds tax (Slice 4)
// - Money is rounded half away from zero to 2 places, ratios to 6 places (V3 rules)
// - Days are counted on UTC calendar dates, as V3 did
// =========================================================
public static class PlatformPricing
{
    public static decimal Money(decimal amount)
    {
        return Math.Round(amount, PlatformSubscriptionLimits.MoneyScale, MidpointRounding.AwayFromZero);
    }

    public static decimal PeriodAmount(PlatformPlanVersion version, PlatformSubscriptionTerm term, int licences)
    {
        ArgumentNullException.ThrowIfNull(version);

        return Money(version.PricePerLicence(term) * licences);
    }

    public static DateTimeOffset PeriodEnd(DateTimeOffset periodStartUtc, PlatformSubscriptionTerm term)
    {
        return term == PlatformSubscriptionTerm.Annual ? periodStartUtc.AddYears(1) : periodStartUtc.AddMonths(1);
    }

    public static decimal RemainingRatio(DateTimeOffset periodStartUtc, DateTimeOffset periodEndUtc, DateTimeOffset utcNow)
    {
        int total = (periodEndUtc.UtcDateTime.Date - periodStartUtc.UtcDateTime.Date).Days;

        if (total <= 0)
        {
            return 0m;
        }

        int left = Math.Clamp((periodEndUtc.UtcDateTime.Date - utcNow.UtcDateTime.Date).Days, 0, total);

        return Math.Round((decimal)left / total, PlatformSubscriptionLimits.RatioScale, MidpointRounding.AwayFromZero);
    }

    public static decimal AddedLicencesCharge(decimal pricePerLicence, int addedLicences, decimal remainingRatio)
    {
        decimal proratedUnit = Money(pricePerLicence * remainingRatio);

        return Money(proratedUnit * Math.Max(0, addedLicences));
    }

    public static decimal UpgradeCharge(decimal currentPeriodAmount, decimal newPeriodAmount, decimal remainingRatio)
    {
        return Math.Max(0m, Money((newPeriodAmount - currentPeriodAmount) * remainingRatio));
    }
}
