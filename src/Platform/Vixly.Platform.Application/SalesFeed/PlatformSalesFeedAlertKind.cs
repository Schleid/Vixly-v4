namespace Vixly.Platform.Application.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedAlertKind.cs
//
// PURPOSE:
// Why staff are alerted about the sales feed.
//
// RESPONSIBILITIES:
// - Name each reason
//
// IMPORTANT NOTES:
// - Stored inside outbox messages; never renumber
// =========================================================
public enum PlatformSalesFeedAlertKind
{
    Parked = 1,
    Stopped = 2,
    NoAnswer = 3
}
