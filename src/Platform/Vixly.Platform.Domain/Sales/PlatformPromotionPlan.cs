namespace Vixly.Platform.Domain.Sales;

// =========================================================
// FILE: PlatformPromotionPlan.cs
//
// PURPOSE:
// One plan a promotion applies to.
//
// RESPONSIBILITIES:
// - Hold the plan id
//
// IMPORTANT NOTES:
// - Owned by PlatformPromotion; no rows means every public plan
// =========================================================
public sealed class PlatformPromotionPlan
{
    private PlatformPromotionPlan()
    {
    }

    internal PlatformPromotionPlan(Guid platformPlanId)
    {
        PlatformPlanId = platformPlanId;
    }

    public Guid PlatformPlanId { get; private set; }
}
