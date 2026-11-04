namespace Vixly.Platform.Domain.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedStatus.cs
//
// PURPOSE:
// Where one sale event stands in the feed to TechOS.
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Parked: TechOS rejected this event; it waits for staff and never blocks the events after it
// =========================================================
public enum PlatformSalesFeedStatus
{
    Pending = 1,
    Accepted = 2,
    Duplicate = 3,
    Parked = 4,

    // Rejected, then replaced by a corrected event under a new id; never sent again.
    Replaced = 5
}
