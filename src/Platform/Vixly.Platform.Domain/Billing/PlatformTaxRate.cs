using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformTaxRate.cs
//
// PURPOSE:
// The tax charged in a country, or in one region of it, from a date (D-B1).
//
// RESPONSIBILITIES:
// - Hold country, optional region, rate in percent, label and the date it applies from
// - Pick the rate for an address on a date
//
// IMPORTANT NOTES:
// - Never changed once saved: a new rate is a new row with a later date, so issued invoices keep their history
// - A region rate wins over the country rate; no rate means 0 % (the invoice says so)
// =========================================================
public sealed class PlatformTaxRate : AuditableEntity
{
    private PlatformTaxRate()
    {
        CountryCode = string.Empty;
        Label = string.Empty;
    }

    private PlatformTaxRate(string countryCode, string? regionCode, decimal rate, string label, DateTimeOffset validFromUtc, Guid createdByUserId, DateTimeOffset utcNow)
        : base(utcNow)
    {
        CountryCode = countryCode;
        RegionCode = regionCode;
        Rate = rate;
        Label = label;
        ValidFromUtc = validFromUtc;
        CreatedByUserId = createdByUserId;
    }

    public string CountryCode { get; private set; }

    public string? RegionCode { get; private set; }

    public decimal Rate { get; private set; }

    public string Label { get; private set; }

    public DateTimeOffset ValidFromUtc { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public static PlatformTaxRate Create(string? countryCode, string? regionCode, decimal rate, string? label, DateTimeOffset validFromUtc, Guid actorUserId, DateTimeOffset utcNow)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformBillingErrors.ActorRequired);
        }

        string country;

        try
        {
            country = PlatformCountryCodes.Normalize(countryCode);
        }
        catch (DomainException)
        {
            throw new DomainException(PlatformBillingErrors.CountryInvalid);
        }

        string? region = Region(regionCode);

        if (rate < 0m || rate > PlatformBillingLimits.MaxRate || decimal.Round(rate, PlatformBillingLimits.RateScale) != rate)
        {
            throw new DomainException(PlatformBillingErrors.TaxRateInvalid);
        }

        string trimmed = label?.Trim() ?? string.Empty;

        if (trimmed.Length == 0 || trimmed.Length > PlatformBillingLimits.TaxLabelMaxLength)
        {
            throw new DomainException(PlatformBillingErrors.TaxLabelRequired);
        }

        if (validFromUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException(PlatformBillingErrors.SettingInvalid);
        }

        return new PlatformTaxRate(country, region, rate, trimmed, validFromUtc, actorUserId, utcNow);
    }

    public static string? Region(string? regionCode)
    {
        string trimmed = regionCode?.Trim().ToUpperInvariant() ?? string.Empty;

        if (trimmed.Length > PlatformBillingLimits.RegionMaxLength)
        {
            throw new DomainException(PlatformBillingErrors.RegionInvalid);
        }

        return trimmed.Length == 0 ? null : trimmed;
    }

    // The rate for an address on a date: the region's newest rate, else the country's.
    public static PlatformTaxRate? Pick(IEnumerable<PlatformTaxRate> rates, string countryCode, string? regionCode, DateTimeOffset atUtc)
    {
        ArgumentNullException.ThrowIfNull(rates);

        string? region = string.IsNullOrWhiteSpace(regionCode) ? null : regionCode.Trim().ToUpperInvariant();
        PlatformTaxRate[] usable = rates
            .Where(rate => rate.CountryCode == countryCode && rate.ValidFromUtc <= atUtc)
            .OrderByDescending(rate => rate.ValidFromUtc)
            .ToArray();

        return (region is null ? null : usable.FirstOrDefault(rate => rate.RegionCode == region))
            ?? usable.FirstOrDefault(rate => rate.RegionCode is null);
    }
}
