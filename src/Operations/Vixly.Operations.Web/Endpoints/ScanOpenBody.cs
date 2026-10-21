namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: ScanOpenBody.cs
//
// PURPOSE:
// What the scan app sends to open a carrier delivery (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the warehouse, carrier and the form reference
//
// IMPORTANT NOTES:
// - Bound from JSON; the use case checks every value
// =========================================================
public sealed record ScanOpenBody(Guid WarehouseId, string? Carrier, string? Key);
