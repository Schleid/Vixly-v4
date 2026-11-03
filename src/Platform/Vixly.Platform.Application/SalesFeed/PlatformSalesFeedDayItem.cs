namespace Vixly.Platform.Application.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedDayItem.cs
//
// PURPOSE:
// One sale event of a UTC day, as the daily totals count it.
//
// RESPONSIBILITIES:
// - Carry the type and the money of one event
//
// IMPORTANT NOTES:
// - Amount and currency are null for subscription.changed (no money)
// =========================================================
public sealed record PlatformSalesFeedDayItem(string EventType, decimal? Amount, string? CurrencyCode);
