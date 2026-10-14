using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Web.Features.Plans;

// =========================================================
// FILE: PlanDraftModel.cs
//
// PURPOSE:
// The draft editor form of a plan version.
//
// RESPONSIBILITIES:
// - Hold the values typed into the draft editor
//
// IMPORTANT NOTES:
// - Mutable for binding; validated by the use case and the domain
// =========================================================
public sealed class PlanDraftModel
{
    public string CurrencyCode { get; set; } = "USD";

    public decimal MonthlyPricePerLicence { get; set; }

    public decimal AnnualPricePerLicence { get; set; }

    public decimal SetupFee { get; set; }

    public int MinimumLicences { get; set; } = PlatformSubscriptionLimits.DefaultMinimumLicences;

    public int? TrialDays { get; set; }

    public int MaxLicensedDevices { get; set; }

    public int MaxWarehouses { get; set; }

    public HashSet<PlatformPlanFeature> Features { get; } = new();
}
