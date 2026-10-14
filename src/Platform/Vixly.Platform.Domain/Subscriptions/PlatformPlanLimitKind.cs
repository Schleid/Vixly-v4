namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformPlanLimitKind.cs
//
// PURPOSE:
// The limits a plan sets (D-S1).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Values kept from V3 SubscriptionLimitType so migrated rows keep their meaning
// =========================================================
public enum PlatformPlanLimitKind
{
    MaxLicensedDevices = 3,
    MaxWarehouses = 4
}
