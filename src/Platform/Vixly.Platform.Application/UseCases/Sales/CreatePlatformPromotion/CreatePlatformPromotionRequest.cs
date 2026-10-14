using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Sales.CreatePlatformPromotion;

// =========================================================
// FILE: CreatePlatformPromotionRequest.cs
//
// PURPOSE:
// Designs a new promotion as a draft (D-S20, D-S26).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CreatePlatformPromotionRequest
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public PlatformDiscountTermsInput? Terms { get; init; }

    public DateTimeOffset ValidFromUtc { get; init; }

    public DateTimeOffset? ValidToUtc { get; init; }

    public IReadOnlyList<Guid> PlanIds { get; init; } = Array.Empty<Guid>();

    public PlatformSubscriptionTerm? Term { get; init; }

    public bool AtRegistration { get; init; }

    public bool ByStaff { get; init; }

    public int? RedemptionLimit { get; init; }
}
