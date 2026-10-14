using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionTarget.cs
//
// PURPOSE:
// A tenant's current subscription and its plan, loaded for a command, or the refusal.
//
// RESPONSIBILITIES:
// - Carry the subscription and its plan, or the error
//
// IMPORTANT NOTES:
// - Returned by PlatformSubscriptionAdministration.LoadAsync
// =========================================================
public sealed class PlatformSubscriptionTarget
{
    private PlatformSubscriptionTarget(PlatformSubscription? subscription, PlatformPlan? plan, Error? error)
    {
        Subscription = subscription;
        Plan = plan;
        Error = error;
    }

    public PlatformSubscription? Subscription { get; }

    public PlatformPlan? Plan { get; }

    public Error? Error { get; }

    public static PlatformSubscriptionTarget Found(PlatformSubscription subscription, PlatformPlan plan)
    {
        return new PlatformSubscriptionTarget(subscription, plan, null);
    }

    public static PlatformSubscriptionTarget Refused(Error error)
    {
        return new PlatformSubscriptionTarget(null, null, error);
    }
}
