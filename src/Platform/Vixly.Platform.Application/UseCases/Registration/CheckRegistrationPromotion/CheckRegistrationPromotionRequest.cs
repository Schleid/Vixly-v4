using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Registration.CheckRegistrationPromotion;

// =========================================================
// FILE: CheckRegistrationPromotionRequest.cs
//
// PURPOSE:
// Checks a promotion code at the plan step of registration (D-S20).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CheckRegistrationPromotionRequest
{
    public string Code { get; init; } = string.Empty;

    public string PlanCode { get; init; } = string.Empty;

    public PlatformSubscriptionTerm Term { get; init; }

    public string? Language { get; init; }
}
