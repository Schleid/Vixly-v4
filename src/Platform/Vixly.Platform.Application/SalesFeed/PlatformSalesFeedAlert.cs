namespace Vixly.Platform.Application.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedAlert.cs
//
// PURPOSE:
// The outbox message that alerts staff about the sales feed.
//
// RESPONSIBILITIES:
// - Carry the reason, how many events it is about and the short answer code
//
// IMPORTANT NOTES:
// - Holds no event body and no secret
// =========================================================
public sealed record PlatformSalesFeedAlert(PlatformSalesFeedAlertKind Kind, int Count, string Code, DateTimeOffset OccurredUtc);
