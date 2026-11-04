namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: ScanBody.cs
//
// PURPOSE:
// What the scan app sends for one scan (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the barcode, the scan reference, the damage key and the chosen printer
//
// IMPORTANT NOTES:
// - Bound from JSON; the use case checks every value
// =========================================================
public sealed record ScanBody(string? Barcode, string? Key, bool Damaged, Guid? PrinterId);
