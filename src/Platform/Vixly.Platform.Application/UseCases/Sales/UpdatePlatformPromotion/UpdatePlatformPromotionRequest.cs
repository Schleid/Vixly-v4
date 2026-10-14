using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Sales.UpdatePlatformPromotion;

// =========================================================
// FILE: UpdatePlatformPromotionRequest.cs
//
// PURPOSE:
// Changes a draft or rejected promotion; it becomes a draft again.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UpdatePlatformPromotionRequest
{
    public Guid PromotionId { get; init; }

    public PlatformDiscountTermsInput? Terms { get; init; }

    public DateTimeOffset ValidFromUtc { get; init; }

    public DateTimeOffset? ValidToUtc { get; init; }

    public IReadOnlyList<Guid> PlanIds { get; init; } = Array.Empty<Guid>();

    public PlatformSubscriptionTerm? Term { get; init; }

    public bool AtRegistration { get; init; }

    public bool ByStaff { get; init; }

    public int? RedemptionLimit { get; init; }
}
