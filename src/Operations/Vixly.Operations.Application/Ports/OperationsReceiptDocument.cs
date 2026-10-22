namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsReceiptDocument.cs
//
// PURPOSE:
// A warehouse receipt as printed for the driver to sign (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the receipt and its packages
//
// IMPORTANT NOTES:
// - Words come from the resources in the reader's language
// =========================================================
public sealed record OperationsReceiptDocument(
    string CompanyName,
    string WarehouseCode,
    string WarehouseName,
    string ReceiptNumber,
    string? Carrier,
    DateTimeOffset OpenedUtc,
    DateTimeOffset ClosedUtc,
    string ReceivedBy,
    int PieceCount,
    int DamagedCount,
    IReadOnlyList<OperationsReceiptLine> Lines);
