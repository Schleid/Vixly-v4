namespace Vixly.Platform.Application.UseCases.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedOverviewItem.cs
//
// PURPOSE:
// How the sales feed to TechOS stands, as the Control sales feed page shows it.
//
// RESPONSIBILITIES:
// - Carry whether it is configured or stopped, the last success, retries and the event counts
// - Carry the parked events, newest first
//
// IMPORTANT NOTES:
// - Plain record; holds no URL and no secret
// =========================================================
public sealed record PlatformSalesFeedOverviewItem(
    bool Configured,
    string? SettingsProblem,
    string? Problem,
    DateTimeOffset? ProblemUtc,
    DateTimeOffset? LastSuccessUtc,
    DateTimeOffset? FailingSinceUtc,
    string? LastFailure,
    DateTimeOffset? NextAttemptUtc,
    int Pending,
    int Parked,
    int Sent,
    IReadOnlyList<PlatformSalesFeedParkedItem> ParkedEvents);
