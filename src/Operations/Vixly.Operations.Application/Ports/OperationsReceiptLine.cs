namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsReceiptLine.cs
//
// PURPOSE:
// One package on a warehouse receipt (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry what the driver and the clerk sign for
//
// IMPORTANT NOTES:
// - The customer code is printed, never the customer's name or phone
// =========================================================
public sealed record OperationsReceiptLine(string ParcelNumber, string? TrackingNumber, string? CustomerCode, bool Damaged);
