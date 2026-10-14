namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformPlanStatus.cs
//
// PURPOSE:
// Whether a plan can still be chosen.
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - A retired plan keeps its subscribers (sweep D-S2 rule)
// =========================================================
public enum PlatformPlanStatus
{
    Active = 0,
    Retired = 1
}
