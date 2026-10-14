namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformPlanVersionPublished.cs
//
// PURPOSE:
// The payload sent when a plan version is published.
//
// RESPONSIBILITIES:
// - Name the plan, the new version and the version it replaces
//
// IMPORTANT NOTES:
// - Read by the price-increase notice handler (D-S13)
// =========================================================
public sealed record PlatformPlanVersionPublished(
    Guid PlatformPlanId,
    string PlanCode,
    Guid VersionId,
    int VersionNumber,
    Guid? PreviousVersionId,
    DateTimeOffset OccurredUtc);
