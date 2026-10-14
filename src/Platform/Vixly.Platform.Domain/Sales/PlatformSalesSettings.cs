using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Sales;

// =========================================================
// FILE: PlatformSalesSettings.cs
//
// PURPOSE:
// What salespeople may do on their own, set by the sales manager (D-S22, D-S25).
//
// RESPONSIBILITIES:
// - Hold the limits, the offer link validity, the discount-ending notice and the registration codes switch
// - Decide whether a discount or an offer needs approval
//
// IMPORTANT NOTES:
// - One row for the platform; every change is audited by the use case
// - Nothing is preset: an empty limit means every such discount needs approval, an empty notice means no email (owner, 2026-10-14)
// - No commercial value lives in code
// =========================================================
public sealed class PlatformSalesSettings : AuditableEntity
{
    public const int MaxDays = 3650;

    public const string SingletonKey = "default";

    private readonly List<PlatformSalesFixedLimit> _fixedLimits = new();

    private PlatformSalesSettings()
    {
        Key = SingletonKey;
    }

    private PlatformSalesSettings(DateTimeOffset utcNow)
        : base(utcNow)
    {
        Key = SingletonKey;
    }

    public string Key { get; private set; }

    public decimal? MaxPercentWithoutApproval { get; private set; }

    public IReadOnlyList<PlatformSalesFixedLimit> FixedLimits => _fixedLimits;

    public bool NoEndWithoutApproval { get; private set; }

    public int? MaxOfferValidityDays { get; private set; }

    public int? DiscountEndNoticeDays { get; private set; }

    public bool PromotionCodesAtRegistration { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static PlatformSalesSettings Empty(DateTimeOffset utcNow)
    {
        return new PlatformSalesSettings(utcNow);
    }

    public void Update(
        decimal? maxPercentWithoutApproval,
        IReadOnlyDictionary<string, decimal> fixedLimits,
        bool noEndWithoutApproval,
        int? maxOfferValidityDays,
        int? discountEndNoticeDays,
        bool promotionCodesAtRegistration,
        Guid actorUserId,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(fixedLimits);

        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformSalesErrors.ActorRequired);
        }

        if (maxPercentWithoutApproval is decimal percent && (percent < 0m || percent > 100m || decimal.Round(percent, 2) != percent))
        {
            throw new DomainException(PlatformSalesErrors.SettingInvalid);
        }

        if (maxOfferValidityDays is < 1 or > MaxDays || discountEndNoticeDays is < 1 or > MaxDays)
        {
            throw new DomainException(PlatformSalesErrors.SettingInvalid);
        }

        List<PlatformSalesFixedLimit> limits = new();

        foreach ((string currency, decimal amount) in fixedLimits)
        {
            if (amount < 0m || amount > PlatformSubscriptionLimits.MaxPrice || decimal.Round(amount, PlatformSubscriptionLimits.MoneyScale) != amount)
            {
                throw new DomainException(PlatformSalesErrors.SettingInvalid);
            }

            limits.Add(new PlatformSalesFixedLimit(PlatformCurrencies.Normalize(currency), amount));
        }

        if (limits.Select(limit => limit.CurrencyCode).Distinct(StringComparer.Ordinal).Count() != limits.Count)
        {
            throw new DomainException(PlatformSalesErrors.SettingInvalid);
        }

        MaxPercentWithoutApproval = maxPercentWithoutApproval;
        _fixedLimits.Clear();
        _fixedLimits.AddRange(limits);
        NoEndWithoutApproval = noEndWithoutApproval;
        MaxOfferValidityDays = maxOfferValidityDays;
        DiscountEndNoticeDays = discountEndNoticeDays;
        PromotionCodesAtRegistration = promotionCodesAtRegistration;
        UpdatedByUserId = actorUserId;
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: APPROVAL (D-S22, D-S24)
    // =====================================================
    public bool DiscountNeedsApproval(PlatformDiscountTerms terms, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(terms);

        if (terms.HasNoEnd && !NoEndWithoutApproval)
        {
            return true;
        }

        return terms.Kind == PlatformDiscountKind.Percent
            ? MaxPercentWithoutApproval is not decimal percent || terms.Value > percent
            : _fixedLimits.FirstOrDefault(limit => limit.CurrencyCode == currencyCode) is not { } limit || terms.Value > limit.Amount;
    }

    public bool OfferNeedsApproval(PlatformDiscountTerms? terms, string currencyCode, DateTimeOffset expiresUtc, DateTimeOffset utcNow)
    {
        if (MaxOfferValidityDays is not int days || expiresUtc > utcNow.AddDays(days))
        {
            return true;
        }

        return terms is not null && DiscountNeedsApproval(terms, currencyCode);
    }
}
