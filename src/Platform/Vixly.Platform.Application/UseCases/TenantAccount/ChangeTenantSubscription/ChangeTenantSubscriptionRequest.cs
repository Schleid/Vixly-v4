using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.TenantAccount.ChangeTenantSubscription;

// =========================================================
// FILE: ChangeTenantSubscriptionRequest.cs
//
// PURPOSE:
// The tenant changes its plan, term or licences by itself (call 21).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeTenantSubscriptionRequest
{
    public Guid PlatformPlanId { get; init; }

    public PlatformSubscriptionTerm Term { get; init; }

    public int Licences { get; init; }
}
