using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.GetPlatformTenantSubscription;

// =========================================================
// FILE: GetPlatformTenantSubscriptionResponse.cs
//
// PURPOSE:
// Reads a tenant's current subscription for the tenant page.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformTenantSubscriptionResponse
{
    private GetPlatformTenantSubscriptionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantSubscriptionDetail? Subscription { get; private init; }

    public PlatformSubscriptionAbilities Abilities { get; private init; } = PlatformSubscriptionAbilities.None;

    public IReadOnlyList<PlatformPlanOption> PlanOptions { get; private init; } = Array.Empty<PlatformPlanOption>();

    public IReadOnlyList<PlatformLimitItem> Limits { get; private init; } = Array.Empty<PlatformLimitItem>();

    public static GetPlatformTenantSubscriptionResponse Success(PlatformTenantSubscriptionDetail? subscription, PlatformSubscriptionAbilities abilities, IReadOnlyList<PlatformPlanOption> planOptions, IReadOnlyList<PlatformLimitItem> limits)
    {
        return new GetPlatformTenantSubscriptionResponse { Succeeded = true, Subscription = subscription, Abilities = abilities, PlanOptions = planOptions, Limits = limits };
    }

    public static GetPlatformTenantSubscriptionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformTenantSubscriptionResponse { Error = error };
    }
}
