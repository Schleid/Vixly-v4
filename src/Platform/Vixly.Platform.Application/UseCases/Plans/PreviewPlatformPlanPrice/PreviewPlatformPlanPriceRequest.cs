using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Plans.PreviewPlatformPlanPrice;

// =========================================================
// FILE: PreviewPlatformPlanPriceRequest.cs
//
// PURPOSE:
// Prices a period of a plan for a term and a number of licences (D-S9).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PreviewPlatformPlanPriceRequest
{
    public Guid PlatformPlanId { get; init; }

    public PlatformSubscriptionTerm Term { get; init; }

    public int Licences { get; init; }
}
