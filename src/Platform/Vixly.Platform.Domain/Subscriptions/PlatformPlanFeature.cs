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
    DataExport = 6,

    // Slice 7 step 4 (R-8, owner 2026-10-21): the company's partners sign in to their own destination warehouse.
    PartnerAgents = 7,

    // Operations Slice 9 (owner, 2026-10-22: the integrations are plan features): notices by SMS and WhatsApp through the company's own Twilio,
    // a one-way sync to QuickBooks Online, the company's API keys and webhooks, carrier data providers.
    TextMessages = 8,
    QuickBooksSync = 9,
    TenantApi = 10,
    CarrierData = 11,

    // Vixly Lite (owner, 2026-10-30): the Operations modules a plan may leave out. Every plan published before keeps them (migration PlanModules).
    CustomerPortal = 12,
    Insurance = 13,
    CreditAccounts = 14,
    Supplies = 15,
    HomeDelivery = 16,
    SupportDesk = 17,
    Payables = 18
}
