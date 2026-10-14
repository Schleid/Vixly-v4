using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ChangePlatformSubscription;

// =========================================================
// FILE: ChangePlatformSubscriptionRequest.cs
//
// PURPOSE:
// Changes plan, term or licences: upgrades and more licences now, the rest at the period end (D-S4, D-S5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangePlatformSubscriptionRequest
{
    public Guid PlatformTenantId { get; init; }

    public Guid PlatformPlanId { get; init; }

    public PlatformSubscriptionTerm Term { get; init; }

    public int Licences { get; init; }
}
