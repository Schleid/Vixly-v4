namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformPlanVersionStatus.cs
//
// PURPOSE:
// The state of one version of a plan.
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - PendingApproval: submitted by sales, waiting for the CFO (D-S26)
// =========================================================
public enum PlatformPlanVersionStatus
{
    Draft = 0,
    Published = 1,
    Superseded = 2,
    PendingApproval = 3
}
