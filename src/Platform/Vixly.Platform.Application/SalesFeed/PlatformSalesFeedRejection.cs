namespace Vixly.Platform.Application.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedRejection.cs
//
// PURPOSE:
// One event TechOS rejected inside a 200 answer.
//
// RESPONSIBILITIES:
// - Carry the event id and TechOS's error
//
// IMPORTANT NOTES:
// - The error is TechOS's text; it is shortened and shown to staff only
// =========================================================
public sealed record PlatformSalesFeedRejection(Guid Id, string Error);
