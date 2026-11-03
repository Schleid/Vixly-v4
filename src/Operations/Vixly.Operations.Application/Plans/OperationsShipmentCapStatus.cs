namespace Vixly.Operations.Application.Plans;

// =========================================================
// FILE: OperationsShipmentCapStatus.cs
//
// PURPOSE:
// Where a company stands against its plan's shipments this month (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Carry the month, the shipments so far, the cap, the extra-shipment price and the warning level
//
// IMPORTANT NOTES:
// - Level: 0, 80, 100, 125 or 150 (percent of the cap); Cap 0 means no cap
// - BlockAt is the last shipment allowed this month (150 percent of the cap)
// =========================================================
public sealed record OperationsShipmentCapStatus(
    string Month,
    int Used,
    int Cap,
    decimal ExtraShipmentPrice,
    string? CurrencyCode,
    int Level,
    bool GraceAvailable,
    bool GraceThisMonth)
{
    public bool Capped => Cap > 0;

    public int BlockAt => OperationsShipmentCap.BlockAt(Cap);

    public int Extra => Capped ? Math.Max(0, Used - Cap) : 0;

    public bool Blocked => Capped && Used >= BlockAt && !GraceAvailable;
}
