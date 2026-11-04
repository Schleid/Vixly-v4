namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsPlanFeatures.cs
//
// PURPOSE:
// What the tenant's plan lets its site show (W-4, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the two branding features of the plan, and partner agents (Slice 7 step 4, R-8)
//
// IMPORTANT NOTES:
// - portal_theme_customization: colours and theme; homepage_content_customization: the site texts and main image
// - Saved choices are kept when a feature is missing; the site falls back to the defaults
// - partner_agents fails closed: when Platform cannot answer, Unknown has it off while the site keeps its look
// =========================================================
public sealed record OperationsPlanFeatures(bool ThemeCustomization, bool ContentCustomization)
{
    public const string Theme = "portal_theme_customization";

    public const string Content = "homepage_content_customization";

    public const string Partners = "partner_agents";

    // Slice 9: the integrations, each a plan feature (owner, 2026-10-22).
    public const string Texts = "text_messages";

    public const string QuickBooks = "quick_books_sync";

    public const string Api = "tenant_api";

    public const string Carriers = "carrier_data";

    public static OperationsPlanFeatures All { get; } = new(true, true) { PartnerAgents = true, TextMessages = true, QuickBooksSync = true, TenantApi = true, CarrierData = true };

    // Fails closed: nothing paid for is used while Platform cannot answer.
    public static OperationsPlanFeatures Unknown { get; } = new(true, true) { PartnerAgents = false, IsKnown = false };

    // Help must distinguish the cosmetic fallback from a verified feature grant.
    public bool IsKnown { get; init; } = true;

    public bool PartnerAgents { get; init; }

    public bool TextMessages { get; init; }

    public bool QuickBooksSync { get; init; }

    public bool TenantApi { get; init; }

    public bool CarrierData { get; init; }

    // Shipment cap (owner, 2026-10-29): shipments a month (0 = no cap), the price of each one above it and the plan's currency.
    public const string ShipmentsLimit = "max_shipments_per_month";

    public int ShipmentCap { get; init; }

    public decimal ExtraShipmentPrice { get; init; }

    public string? CurrencyCode { get; init; }

    // Vixly Lite (owner, 2026-10-30): the Operations modules the plan leaves out (OperationsPlanModules). Empty means every module.
    public IReadOnlySet<string> ModulesOff { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    public bool Includes(string module) => !ModulesOff.Contains(module);

    public bool Limited => !ThemeCustomization || !ContentCustomization;
}
