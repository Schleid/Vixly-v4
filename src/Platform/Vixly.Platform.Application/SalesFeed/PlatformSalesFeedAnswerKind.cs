namespace Vixly.Platform.Application.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedAnswerKind.cs
//
// PURPOSE:
// What TechOS's answer to a batch means for the sender.
//
// RESPONSIBILITIES:
// - Name each outcome
//
// IMPORTANT NOTES:
// - Read: 200 with accepted, duplicates and rejected lists
// - TooLarge: 413; the sender halves the batch
// - Refused: 400 or 401; a configuration problem, sending stops until staff try again
// - Retry: 404 (source off), 5xx, any other answer or none; retried with backoff
// =========================================================
public enum PlatformSalesFeedAnswerKind
{
    Read = 1,
    TooLarge = 2,
    Refused = 3,
    Retry = 4
}
