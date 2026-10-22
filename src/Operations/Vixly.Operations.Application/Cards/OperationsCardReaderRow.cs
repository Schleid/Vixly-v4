namespace Vixly.Operations.Application.Cards;

// =========================================================
// FILE: OperationsCardReaderRow.cs
//
// PURPOSE:
// A card reader as Settings → Payments and the cashier show it (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry its name, Stripe's id and its warehouse
//
// IMPORTANT NOTES:
// - Read-only
// =========================================================
public sealed record OperationsCardReaderRow(Guid ReaderId, string Label, string StripeReaderId, Guid WarehouseId, string WarehouseName);
