using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Web.Features.Subscriptions;

// =========================================================
// FILE: TenantSubscriptionModel.cs
//
// PURPOSE:
// The start or change form of a tenant subscription.
//
// RESPONSIBILITIES:
// - Hold the chosen plan, term, licences and trial
//
// IMPORTANT NOTES:
// - Mutable for binding; validated by the use case and the domain
// =========================================================
public sealed class TenantSubscriptionModel
{
    public Guid PlatformPlanId { get; set; }

    public PlatformSubscriptionTerm Term { get; set; } = PlatformSubscriptionTerm.Monthly;

    public int Licences { get; set; } = PlatformSubscriptionLimits.DefaultMinimumLicences;

    public bool StartTrial { get; set; }
}
