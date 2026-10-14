namespace Vixly.Platform.Application.UseCases.Plans.ReactivatePlatformPlan;

// =========================================================
// FILE: ReactivatePlatformPlanRequest.cs
//
// PURPOSE:
// Opens a retired plan to new subscriptions again.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReactivatePlatformPlanRequest
{
    public Guid PlatformPlanId { get; init; }
}
