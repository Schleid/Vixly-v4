using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: OfferFormModel.cs
//
// PURPOSE:
// The offer link form (D-S21).
//
// RESPONSIBILITIES:
// - Hold the agreed terms as typed
//
// IMPORTANT NOTES:
// - The expiry is the end of the chosen day, UTC
// =========================================================
public sealed class OfferFormModel
{
    public string? CompanyName { get; set; }

    public string? BoundEmail { get; set; }

    public string? Reason { get; set; }

    public string PlanId { get; set; } = string.Empty;

    public PlatformSubscriptionTerm Term { get; set; } = PlatformSubscriptionTerm.Monthly;

    public int Licences { get; set; } = 2;

    public bool StartTrial { get; set; }

    public bool HasDiscount { get; set; } = true;

    public DiscountTermsModel Discount { get; set; } = new();

    public decimal? SetupFeeOverride { get; set; }

    public List<ChargeRow> Charges { get; } = new();

    public DateTime? ExpiresOn { get; set; }

    public int UsesAllowed { get; set; } = 1;

    public Guid PlatformPlanId => Guid.TryParse(PlanId, out Guid id) ? id : Guid.Empty;

    public IReadOnlyList<PlatformOfferChargeItem> ChargeItems()
    {
        return Charges.Where(row => !string.IsNullOrWhiteSpace(row.Description)).Select(row => new PlatformOfferChargeItem(row.Description!.Trim(), row.Amount)).ToArray();
    }

    public static OfferFormModel From(PlatformOfferDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);

        OfferFormModel model = new()
        {
            CompanyName = detail.CompanyName,
            BoundEmail = detail.BoundEmail,
            Reason = detail.Reason,
            PlanId = detail.PlatformPlanId.ToString(),
            Term = detail.Term,
            Licences = detail.Licences,
            StartTrial = detail.StartTrial,
            HasDiscount = detail.Discount is not null,
            Discount = DiscountTermsModel.From(detail.Discount),
            SetupFeeOverride = detail.SetupFeeOverride is decimal fee ? Math.Round(fee, 2) : null,
            ExpiresOn = detail.ExpiresUtc.UtcDateTime.Date,
            UsesAllowed = detail.UsesAllowed
        };
        model.Charges.AddRange(detail.Charges.Select(item => new ChargeRow { Description = item.Description, Amount = Math.Round(item.Amount, 2) }));

        return model;
    }

    public sealed class ChargeRow
    {
        public string? Description { get; set; }

        public decimal Amount { get; set; }
    }
}
