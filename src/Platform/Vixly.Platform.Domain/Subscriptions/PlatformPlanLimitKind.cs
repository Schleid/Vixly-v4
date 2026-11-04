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
// - MaxShipmentsPerMonth: 0 (or no row, as on versions published before it) means no cap
// =========================================================
public enum PlatformPlanLimitKind
{
    MaxLicensedDevices = 3,
    MaxWarehouses = 4,

    // Shipment cap (owner, 2026-10-29): shipments a company may create in a calendar month; 0 means no cap.
    MaxShipmentsPerMonth = 5
}
