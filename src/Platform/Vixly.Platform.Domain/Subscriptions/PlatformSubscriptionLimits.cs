namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionLimits.cs
//
// PURPOSE:
// Sizes and defaults of the plans and subscriptions model.
//
// RESPONSIBILITIES:
// - Keep every length, range and default in one place
//
// IMPORTANT NOTES:
// - Database column sizes come from here
// =========================================================
public static class PlatformSubscriptionLimits
{
    public const int PlanCodeMinLength = 2;

    public const int PlanCodeMaxLength = 32;

    public const int PlanNameMaxLength = 100;

    public const int PlanDescriptionMaxLength = 1000;

    public const int CurrencyCodeLength = 3;

    public const int DefaultMinimumLicences = 2;

    public const int MaxLicences = 100000;

    public const int MaxLimitValue = 1000000;

    public const int MaxTrialDays = 365;

    public const int MaxDisplayOrder = 1000;

    public const int ReasonMaxLength = 500;

    public const int EventCodeMaxLength = 100;

    public const int MoneyScale = 2;

    public const int RatioScale = 6;

    public const decimal MaxPrice = 1000000m;

    public static readonly TimeSpan MaxTrialExtension = TimeSpan.FromDays(365);
}
