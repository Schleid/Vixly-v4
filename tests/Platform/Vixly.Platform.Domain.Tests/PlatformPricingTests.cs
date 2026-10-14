using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformPricingTests.cs
//
// PURPOSE:
// Proves the pricing arithmetic matches V3 (D-S9).
//
// RESPONSIBILITIES:
// - Period amounts, period ends, remaining ratio and the two proration formulas
//
// IMPORTANT NOTES:
// - Money rounds half away from zero to 2 places, ratios to 6 places
// =========================================================
public sealed class PlatformPricingTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PeriodEnd_IsOneMonthOrOneYearLater()
    {
        Assert.Equal(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero), PlatformPricing.PeriodEnd(Start, PlatformSubscriptionTerm.Monthly));
        Assert.Equal(new DateTimeOffset(2027, 10, 1, 0, 0, 0, TimeSpan.Zero), PlatformPricing.PeriodEnd(Start, PlatformSubscriptionTerm.Annual));
    }

    [Fact]
    public void RemainingRatio_CountsCalendarDays_RoundedToSixPlaces()
    {
        DateTimeOffset end = PlatformPricing.PeriodEnd(Start, PlatformSubscriptionTerm.Monthly);

        Assert.Equal(0.516129m, PlatformPricing.RemainingRatio(Start, end, new DateTimeOffset(2026, 10, 16, 15, 0, 0, TimeSpan.Zero)));
        Assert.Equal(1m, PlatformPricing.RemainingRatio(Start, end, Start));
        Assert.Equal(0m, PlatformPricing.RemainingRatio(Start, end, end.AddDays(3)));
    }

    [Fact]
    public void AddedLicences_UseTheProratedUnitPrice_LikeV3CapacityExpansion()
    {
        Assert.Equal(10.32m, PlatformPricing.AddedLicencesCharge(10m, 2, 0.516129m));
    }

    [Fact]
    public void Upgrade_ChargesTheProratedDifference_AndNeverANegativeAmount()
    {
        Assert.Equal(15.48m, PlatformPricing.UpgradeCharge(30m, 60m, 0.516129m));
        Assert.Equal(0m, PlatformPricing.UpgradeCharge(60m, 30m, 0.5m));
    }

    [Fact]
    public void Money_RoundsHalfAwayFromZero()
    {
        Assert.Equal(0.13m, PlatformPricing.Money(0.125m));
        Assert.Equal(2.68m, PlatformPricing.Money(2.675m));
    }
}
