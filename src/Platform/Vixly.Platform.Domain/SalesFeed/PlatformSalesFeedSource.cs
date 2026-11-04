namespace Vixly.Platform.Domain.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedSource.cs
//
// PURPOSE:
// The record a sale event was made from.
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - InvoiceEvent and SubscriptionEvent rows are the history journals written in the same save as the sale
// =========================================================
public enum PlatformSalesFeedSource
{
    InvoiceEvent = 1,
    SubscriptionEvent = 2,
    DailyTotals = 3,

    // A corrected copy of a rejected event: SourceId is the event it replaces (2026-11-03).
    Correction = 4
}
