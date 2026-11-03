namespace Vixly.Operations.Application.Items;

// =========================================================
// FILE: OperationsItemReviewRow.cs
//
// PURPOSE:
// A parcel waiting for its items to be classified (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Carry the parcel, its warehouse, when it came in, what was declared and how many items still miss a code or origin
//
// IMPORTANT NOTES:
// - Zero items: nothing declared item by item yet
// =========================================================
public sealed record OperationsItemReviewRow(Guid ParcelId, string Number, string WarehouseCode, DateTimeOffset CreatedUtc, string? Contents, int Items, int Missing);
