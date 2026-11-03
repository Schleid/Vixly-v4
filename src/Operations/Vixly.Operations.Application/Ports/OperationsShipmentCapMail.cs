using Vixly.Shared.Kernel.Localization;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsShipmentCapMail.cs
//
// PURPOSE:
// A plan warning to the Owner: the shipments this month against the plan's cap (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Carry the Owner, the company, the level and the numbers
//
// IMPORTANT NOTES:
// - Written for the company's Owner; the plan's provider is not named
// =========================================================
public sealed record OperationsShipmentCapMail(
    string ToAddress,
    string ToName,
    LanguageCode Language,
    string CompanyName,
    string Month,
    int Level,
    int Used,
    int Cap,
    int BlockAt,
    string ExtraPrice,
    bool GraceMonth,
    string Link);
