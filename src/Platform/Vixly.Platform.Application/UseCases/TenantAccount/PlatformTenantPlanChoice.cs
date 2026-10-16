using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.TenantAccount;

// =========================================================
// FILE: PlatformTenantPlanChoice.cs
//
// PURPOSE:
// Which plans a tenant may move to by itself: the public plans on sale, or its current plan (Slice 9).
//
// RESPONSIBILITIES:
// - Find the target plan or refuse it
//
// IMPORTANT NOTES:
// - Private plans and offers stay with Vixly sales staff
// =========================================================
public static class PlatformTenantPlanChoice
{
    public static async Task<PlatformPlan?> TargetAsync(
        PlatformSubscriptionAdministration administration,
        PlatformPlan current,
        Guid targetPlanId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(administration);
        ArgumentNullException.ThrowIfNull(current);

        if (targetPlanId == current.Id)
        {
            return current;
        }

        return (await administration.Plans.ListChoosableAsync(publicOnly: true, cancellationToken)).FirstOrDefault(plan => plan.Id == targetPlanId);
    }

    public static PlatformPlanVersion Ceiling(PlatformSubscription subscription, PlatformPlan target)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(target);

        return target.Id == subscription.PlatformPlanId
            ? target.Current ?? target.Version(subscription.PlanVersionId)
            : target.Current ?? target.Versions[0];
    }
}
