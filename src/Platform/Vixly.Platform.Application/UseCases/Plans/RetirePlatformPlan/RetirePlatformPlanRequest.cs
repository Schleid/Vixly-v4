namespace Vixly.Platform.Application.UseCases.Plans.RetirePlatformPlan;

// =========================================================
// FILE: RetirePlatformPlanRequest.cs
//
// PURPOSE:
// Retires a plan: no new subscriptions; subscribers stay on it.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RetirePlatformPlanRequest
{
    public Guid PlatformPlanId { get; init; }
}
