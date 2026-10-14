using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales;

// =========================================================
// FILE: PlatformOfferPlanCheck.cs
//
// PURPOSE:
// Checks that an offer's plan, term, licences and trial can be started.
//
// RESPONSIBILITIES:
// - Refuse an offer the prospect could not use
//
// IMPORTANT NOTES:
// - The same rules apply again when the prospect uses the link
// - A private plan is allowed: offers are how private plans are sold
// =========================================================
public static class PlatformOfferPlanCheck
{
    public static Error? Refusal(PlatformPlan? plan, PlatformSubscriptionTerm term, int licences, bool startTrial)
    {
        if (plan is null || !plan.CanBeChosen)
        {
            return PlatformSubscriptionErrors.PlanNotAvailable;
        }

        PlatformPlanVersion version = plan.Current!;

        if (!version.Offers(term))
        {
            return PlatformSubscriptionErrors.TermNotOffered;
        }

        if (licences < version.MinimumLicences)
        {
            return PlatformSubscriptionErrors.BelowMinimum(version.MinimumLicences);
        }

        int max = version.Limit(PlatformPlanLimitKind.MaxLicensedDevices);

        if (licences > max)
        {
            return PlatformSubscriptionErrors.AboveLimit(max);
        }

        return startTrial && !version.OffersTrial ? PlatformSubscriptionErrors.TrialNotOffered : null;
    }
}
