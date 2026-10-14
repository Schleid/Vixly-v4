using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformPlanVersionTerms.cs
//
// PURPOSE:
// The commercial terms of a plan version, checked as one value.
//
// RESPONSIBILITIES:
// - Validate prices, setup fee, currency, minimum licences, trial days, limits and features together
//
// IMPORTANT NOTES:
// - Prices are before tax (D-S9)
// - The annual price is stated per year (D-S7; V3 stored a monthly rate)
// - A price of zero means the term is not offered
// =========================================================
public sealed record PlatformPlanVersionTerms
{
    private PlatformPlanVersionTerms(
        string currencyCode,
        decimal monthlyPricePerLicence,
        decimal annualPricePerLicence,
        decimal setupFee,
        int minimumLicences,
        int? trialDays,
        int maxLicensedDevices,
        int maxWarehouses,
        IReadOnlyCollection<PlatformPlanFeature> features)
    {
        CurrencyCode = currencyCode;
        MonthlyPricePerLicence = monthlyPricePerLicence;
        AnnualPricePerLicence = annualPricePerLicence;
        SetupFee = setupFee;
        MinimumLicences = minimumLicences;
        TrialDays = trialDays;
        MaxLicensedDevices = maxLicensedDevices;
        MaxWarehouses = maxWarehouses;
        Features = features;
    }

    public string CurrencyCode { get; }

    public decimal MonthlyPricePerLicence { get; }

    public decimal AnnualPricePerLicence { get; }

    public decimal SetupFee { get; }

    public int MinimumLicences { get; }

    public int? TrialDays { get; }

    public int MaxLicensedDevices { get; }

    public int MaxWarehouses { get; }

    public IReadOnlyCollection<PlatformPlanFeature> Features { get; }

    public static PlatformPlanVersionTerms Create(
        string? currencyCode,
        decimal monthlyPricePerLicence,
        decimal annualPricePerLicence,
        decimal setupFee,
        int minimumLicences,
        int? trialDays,
        int maxLicensedDevices,
        int maxWarehouses,
        IEnumerable<PlatformPlanFeature>? features)
    {
        string currency = PlatformCurrencies.Normalize(currencyCode);

        decimal monthly = Price(monthlyPricePerLicence);
        decimal annual = Price(annualPricePerLicence);
        decimal setup = Price(setupFee);

        if (minimumLicences < 1 || minimumLicences > PlatformSubscriptionLimits.MaxLicences)
        {
            throw new DomainException(PlatformPlanErrors.MinimumLicencesInvalid);
        }

        int? trial = trialDays is null or 0 ? null : trialDays;

        if (trial is < 0 or > PlatformSubscriptionLimits.MaxTrialDays)
        {
            throw new DomainException(PlatformPlanErrors.TrialDaysInvalid);
        }

        int devices = Limit(maxLicensedDevices);
        int warehouses = Limit(maxWarehouses);

        if (minimumLicences > devices)
        {
            throw new DomainException(PlatformPlanErrors.MinimumAboveDeviceLimit);
        }

        PlatformPlanFeature[] featureSet = (features ?? Array.Empty<PlatformPlanFeature>()).Distinct().OrderBy(feature => feature).ToArray();

        if (featureSet.Any(feature => !Enum.IsDefined(feature)))
        {
            throw new DomainException(PlatformPlanErrors.FeatureInvalid);
        }

        return new PlatformPlanVersionTerms(currency, monthly, annual, setup, minimumLicences, trial, devices, warehouses, featureSet);
    }

    public static PlatformPlanVersionTerms Empty(string currencyCode)
    {
        return Create(currencyCode, 0m, 0m, 0m, PlatformSubscriptionLimits.DefaultMinimumLicences, null, PlatformSubscriptionLimits.DefaultMinimumLicences, 0, null);
    }

    public bool HasAnyPrice => MonthlyPricePerLicence > 0m || AnnualPricePerLicence > 0m;

    private static decimal Price(decimal value)
    {
        if (value < 0m || value > PlatformSubscriptionLimits.MaxPrice || decimal.Round(value, 4) != value)
        {
            throw new DomainException(PlatformPlanErrors.PriceInvalid);
        }

        return value;
    }

    private static int Limit(int value)
    {
        if (value < 0 || value > PlatformSubscriptionLimits.MaxLimitValue)
        {
            throw new DomainException(PlatformPlanErrors.LimitInvalid);
        }

        return value;
    }
}
