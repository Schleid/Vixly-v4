using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Subscriptions.PreviewPlatformSubscriptionChange;

// =========================================================
// FILE: PreviewPlatformSubscriptionChangeRequest.cs
//
// PURPOSE:
// Shows what a plan, term or licence change would do and cost (D-S4, D-S5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PreviewPlatformSubscriptionChangeRequest
{
    public Guid PlatformTenantId { get; init; }

    public Guid PlatformPlanId { get; init; }

    public PlatformSubscriptionTerm Term { get; init; }

    public int Licences { get; init; }
}
