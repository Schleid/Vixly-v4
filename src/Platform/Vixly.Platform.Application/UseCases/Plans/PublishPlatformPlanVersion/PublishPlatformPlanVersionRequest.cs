namespace Vixly.Platform.Application.UseCases.Plans.PublishPlatformPlanVersion;

// =========================================================
// FILE: PublishPlatformPlanVersionRequest.cs
//
// PURPOSE:
// Publishes the draft version of a plan (D-S2, D-S13).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PublishPlatformPlanVersionRequest
{
    public Guid PlatformPlanId { get; init; }
}
