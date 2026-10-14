using Vixly.Platform.Application.UseCases.Sales;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: SalesSettingsModel.cs
//
// PURPOSE:
// The Sales settings form (D-S22, D-S25).
//
// RESPONSIBILITIES:
// - Hold every setting as typed, empty when not set
//
// IMPORTANT NOTES:
// - Nothing is preset (owner, 2026-10-14)
// =========================================================
public sealed class SalesSettingsModel
{
    public decimal? MaxPercentWithoutApproval { get; set; }

    public List<FixedLimitRow> FixedLimits { get; } = new();

    public bool NoEndWithoutApproval { get; set; }

    public int? MaxOfferValidityDays { get; set; }

    public int? DiscountEndNoticeDays { get; set; }

    public bool PromotionCodesAtRegistration { get; set; }

    public static SalesSettingsModel From(PlatformSalesSettingsItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        SalesSettingsModel model = new()
        {
            MaxPercentWithoutApproval = item.MaxPercentWithoutApproval,
            NoEndWithoutApproval = item.NoEndWithoutApproval,
            MaxOfferValidityDays = item.MaxOfferValidityDays,
            DiscountEndNoticeDays = item.DiscountEndNoticeDays,
            PromotionCodesAtRegistration = item.PromotionCodesAtRegistration
        };
        model.FixedLimits.AddRange(item.FixedLimits.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new FixedLimitRow { CurrencyCode = pair.Key, Amount = Math.Round(pair.Value, 2) }));

        return model;
    }

    public sealed class FixedLimitRow
    {
        public string CurrencyCode { get; set; } = string.Empty;

        public decimal Amount { get; set; }
    }
}
