using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: PromotionFormModel.cs
//
// PURPOSE:
// The promotion form (D-S20).
//
// RESPONSIBILITIES:
// - Hold the promotion fields as typed
//
// IMPORTANT NOTES:
// - Dates are whole days, UTC
// =========================================================
public sealed class PromotionFormModel
{
    public string? Code { get; set; }

    public string? Name { get; set; }

    public DiscountTermsModel Terms { get; set; } = new();

    public DateTime? ValidFrom { get; set; } = DateTime.UtcNow.Date;

    public DateTime? ValidTo { get; set; }

    public HashSet<Guid> PlanIds { get; } = new();

    public PlatformSubscriptionTerm? Term { get; set; }

    public bool AtRegistration { get; set; } = true;

    public bool ByStaff { get; set; } = true;

    public int? RedemptionLimit { get; set; }

    public static PromotionFormModel From(PlatformPromotionDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);

        PromotionFormModel model = new()
        {
            Code = detail.Code,
            Name = detail.Name,
            Terms = DiscountTermsModel.From(detail.Terms),
            ValidFrom = detail.ValidFromUtc.UtcDateTime.Date,
            ValidTo = detail.ValidToUtc?.UtcDateTime.Date.AddDays(-1),
            Term = detail.Term,
            AtRegistration = detail.AtRegistration,
            ByStaff = detail.ByStaff,
            RedemptionLimit = detail.RedemptionLimit
        };
        model.PlanIds.UnionWith(detail.PlanIds);

        return model;
    }
}
