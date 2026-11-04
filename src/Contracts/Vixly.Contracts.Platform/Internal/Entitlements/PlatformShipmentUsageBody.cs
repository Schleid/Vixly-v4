namespace Vixly.Contracts.Platform.Internal.Entitlements;

// =========================================================
// FILE: PlatformShipmentUsageBody.cs
//
// PURPOSE:
// The shipments a company created in one month, sent by Operations (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Carry the month ("2026-10"), the count and whether the month is over
//
// IMPORTANT NOTES:
// - The month follows the company's time zone; a final report closes the month and charges what is above the cap
// =========================================================
public sealed record PlatformShipmentUsageBody(string Month, int Shipments, bool IsFinal);
