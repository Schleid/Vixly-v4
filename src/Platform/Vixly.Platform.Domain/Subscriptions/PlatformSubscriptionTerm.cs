namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionTerm.cs
//
// PURPOSE:
// How often a subscription is billed (D-S7).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - V3 also had OneTime; none was sold, so it is dropped
// =========================================================
public enum PlatformSubscriptionTerm
{
    Monthly = 1,
    Annual = 2
}
