using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.UseCases.Sales;

// =========================================================
// FILE: PlatformSalesSettingsItem.cs
//
// PURPOSE:
// The Sales settings for the console (D-S22).
//
// RESPONSIBILITIES:
// - Carry every setting, empty when not set
//
// IMPORTANT NOTES:
// - Nothing is preset (owner, 2026-10-14)
// =========================================================
public sealed record PlatformSalesSettingsItem(
    decimal? MaxPercentWithoutApproval,
    IReadOnlyDictionary<string, decimal> FixedLimits,
    bool NoEndWithoutApproval,
    int? MaxOfferValidityDays,
    int? DiscountEndNoticeDays,
    bool PromotionCodesAtRegistration,
    string? UpdatedByName,
    DateTimeOffset? UpdatedUtc)
{
    public static PlatformSalesSettingsItem Empty { get; } = new(null, new Dictionary<string, decimal>(), false, null, null, false, null, null);

    public static PlatformSalesSettingsItem From(PlatformSalesSettings? settings, string? updatedByName)
    {
        return settings is null
            ? Empty
            : new PlatformSalesSettingsItem(
                settings.MaxPercentWithoutApproval,
                settings.FixedLimits.ToDictionary(limit => limit.CurrencyCode, limit => limit.Amount, StringComparer.Ordinal),
                settings.NoEndWithoutApproval,
                settings.MaxOfferValidityDays,
                settings.DiscountEndNoticeDays,
                settings.PromotionCodesAtRegistration,
                updatedByName,
                settings.LastModifiedUtc ?? settings.CreatedUtc);
    }
}
