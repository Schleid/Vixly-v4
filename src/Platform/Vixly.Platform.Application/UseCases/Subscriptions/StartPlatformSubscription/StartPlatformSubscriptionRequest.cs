using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Subscriptions.StartPlatformSubscription;

// =========================================================
// FILE: StartPlatformSubscriptionRequest.cs
//
// PURPOSE:
// Starts a subscription for a tenant without one (D-S15).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class StartPlatformSubscriptionRequest
{
    public Guid PlatformTenantId { get; init; }

    public Guid PlatformPlanId { get; init; }

    public PlatformSubscriptionTerm Term { get; init; }

    public int Licences { get; init; }

    public bool StartTrial { get; init; }
}
