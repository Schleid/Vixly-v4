using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Registration.SelectRegistrationPlan;

// =========================================================
// FILE: SelectRegistrationPlanRequest.cs
//
// PURPOSE:
// Records the plan a registering company chooses, and starts its subscription, with an offer link or a promotion code (D-S12, D-S20, D-S21).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SelectRegistrationPlanRequest
{
    public string AccessToken { get; init; } = string.Empty;

    public string PlanCode { get; init; } = string.Empty;

    public PlatformSubscriptionTerm Term { get; init; }

    public int Licences { get; init; }

    public bool StartTrial { get; init; }

    public string? PromotionCode { get; init; }

    public string? OfferToken { get; init; }
}
