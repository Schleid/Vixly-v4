using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.TenantAccount.PreviewTenantSubscriptionChange;

// =========================================================
// FILE: PreviewTenantSubscriptionChangeRequest.cs
//
// PURPOSE:
// What a change of plan, term or licences would cost the tenant, before it confirms (call 21).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PreviewTenantSubscriptionChangeRequest
{
    public Guid PlatformPlanId { get; init; }

    public PlatformSubscriptionTerm Term { get; init; }

    public int Licences { get; init; }
}
