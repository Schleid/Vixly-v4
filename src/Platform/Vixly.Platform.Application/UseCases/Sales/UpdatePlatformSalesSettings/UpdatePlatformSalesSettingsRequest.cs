namespace Vixly.Platform.Application.UseCases.Sales.UpdatePlatformSalesSettings;

// =========================================================
// FILE: UpdatePlatformSalesSettingsRequest.cs
//
// PURPOSE:
// Changes the Sales settings: limits, offer validity, discount-ending notice, codes at registration (D-S22, D-S25).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UpdatePlatformSalesSettingsRequest
{
    public decimal? MaxPercentWithoutApproval { get; init; }

    public IReadOnlyDictionary<string, decimal> FixedLimits { get; init; } = new Dictionary<string, decimal>();

    public bool NoEndWithoutApproval { get; init; }

    public int? MaxOfferValidityDays { get; init; }

    public int? DiscountEndNoticeDays { get; init; }

    public bool PromotionCodesAtRegistration { get; init; }
}
