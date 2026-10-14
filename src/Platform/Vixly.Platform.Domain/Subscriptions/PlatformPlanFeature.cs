namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformPlanFeature.cs
//
// PURPOSE:
// The features a plan can include (D-S1).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Values kept from V3 SubscriptionFeatureType
// - The white-label features are the entitlement ADR-0012 refers to
// =========================================================
public enum PlatformPlanFeature
{
    HomepageContentCustomization = 1,
    PortalThemeCustomization = 2,
    CustomDomain = 3,
    WhiteLabelPortal = 4,
    AdvancedAnalytics = 5,
    DataExport = 6
}
