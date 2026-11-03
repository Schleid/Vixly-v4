using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedState.cs
//
// PURPOSE:
// How the feed to TechOS is doing: last success, retries and a stop on a configuration problem.
//
// RESPONSIBILITIES:
// - Back off after a failed attempt (404, 5xx, no answer) and say when staff must be alerted (after 24 hours)
// - Stop sending on a configuration problem (400 or 401) until staff try again
//
// IMPORTANT NOTES:
// - One row
// - A retry never parks events: they stay pending until TechOS answers
// - Each alert is raised once per failure spell
// =========================================================
public sealed class PlatformSalesFeedState : AuditableEntity
{
    private PlatformSalesFeedState()
    {
        Key = PlatformSalesFeedLimits.SingletonKey;
    }

    private PlatformSalesFeedState(DateTimeOffset utcNow)
        : base(utcNow)
    {
        Key = PlatformSalesFeedLimits.SingletonKey;
    }

    public string Key { get; private set; }

    public DateTimeOffset? LastSuccessUtc { get; private set; }

    public DateTimeOffset? FailingSinceUtc { get; private set; }

    public int FailedAttempts { get; private set; }

    public DateTimeOffset? NextAttemptUtc { get; private set; }

    // Short code of the last failure ("http_503", "no_answer"...).
    public string? LastFailure { get; private set; }

    // Set when sending is stopped on a configuration problem ("http_400", "http_401").
    public string? Problem { get; private set; }

    public DateTimeOffset? ProblemUtc { get; private set; }

    public DateTimeOffset? AlertedUtc { get; private set; }

    public bool Stopped => Problem is not null;

    public static PlatformSalesFeedState Create(DateTimeOffset utcNow)
    {
        return new PlatformSalesFeedState(utcNow);
    }

    public bool Due(DateTimeOffset utcNow)
    {
        return !Stopped && (NextAttemptUtc is null || NextAttemptUtc <= utcNow);
    }

    public void Succeeded(DateTimeOffset utcNow)
    {
        LastSuccessUtc = utcNow;
        FailingSinceUtc = null;
        FailedAttempts = 0;
        NextAttemptUtc = null;
        LastFailure = null;
        AlertedUtc = null;
        MarkAsModified(utcNow);
    }

    // Returns true when staff must be alerted now (24 hours without an answer, once per spell).
    public bool Failed(string? failure, DateTimeOffset utcNow)
    {
        FailingSinceUtc ??= utcNow;
        FailedAttempts++;
        LastFailure = PlatformSalesFeedLimits.Text(failure, PlatformSalesFeedLimits.ProblemMaxLength);
        IReadOnlyList<TimeSpan> steps = PlatformSalesFeedLimits.Backoff;
        NextAttemptUtc = utcNow + steps[Math.Min(FailedAttempts, steps.Count) - 1];
        MarkAsModified(utcNow);

        if (AlertedUtc is null && utcNow - FailingSinceUtc.Value >= PlatformSalesFeedLimits.RetryWindow)
        {
            AlertedUtc = utcNow;
            return true;
        }

        return false;
    }

    public void Stop(string? problem, DateTimeOffset utcNow)
    {
        Problem = PlatformSalesFeedLimits.Text(problem, PlatformSalesFeedLimits.ProblemMaxLength);
        ProblemUtc = utcNow;
        AlertedUtc = utcNow;
        NextAttemptUtc = null;
        MarkAsModified(utcNow);
    }

    // Staff fixed the configuration: send at the next run.
    public void TryAgain(DateTimeOffset utcNow)
    {
        Problem = null;
        ProblemUtc = null;
        AlertedUtc = null;
        FailingSinceUtc = null;
        FailedAttempts = 0;
        NextAttemptUtc = null;
        LastFailure = null;
        MarkAsModified(utcNow);
    }
}
