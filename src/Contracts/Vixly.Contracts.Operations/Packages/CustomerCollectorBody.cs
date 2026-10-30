namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerCollectorBody.cs
//
// PURPOSE:
// Who the customer names to collect a shipment (Slice 7 step 2, R-6).
//
// RESPONSIBILITIES:
// - Carry the name and phone, or none to clear them
//
// IMPORTANT NOTES:
// - Validated by Operations, as the recipient
// =========================================================
public sealed record CustomerCollectorBody(string? Name, string? Phone);
