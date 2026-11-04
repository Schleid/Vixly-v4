namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsPriceItemView.cs
//
// PURPOSE:
// A price per item on a price list by size (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Carry the values a pricing page shows
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsPriceItemView(string Name, decimal Price);
