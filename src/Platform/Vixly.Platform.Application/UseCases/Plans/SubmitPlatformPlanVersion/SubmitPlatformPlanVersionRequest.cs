namespace Vixly.Platform.Application.UseCases.Plans.SubmitPlatformPlanVersion;

// =========================================================
// FILE: SubmitPlatformPlanVersionRequest.cs
//
// PURPOSE:
// Submits a plan's draft version for CFO approval (D-S26).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SubmitPlatformPlanVersionRequest
{
    public Guid PlatformPlanId { get; init; }
}
