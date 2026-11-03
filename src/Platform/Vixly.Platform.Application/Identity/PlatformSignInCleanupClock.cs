using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Application.Identity;

// =========================================================
// FILE: PlatformSignInCleanupClock.cs
//
// PURPOSE:
// Keeps the sign-in tables from growing forever (background
// jobs comparison, 2026-10-22: V3 purged its reset tokens,
// V4 did not).
//
// RESPONSIBILITIES:
// - Sessions go a year after they expired: they are the
//   sign-in history of Control's users page
// - Challenges, ceremonies and links go 30 days after they
//   expired: an expired one is refused either way
//
// IMPORTANT NOTES:
// - Runs in the subscription clock loop; safe to run twice
// =========================================================
public sealed class PlatformSignInCleanupClock
{
    public static readonly TimeSpan SessionRetention = TimeSpan.FromDays(365);

    public static readonly TimeSpan LinkRetention = TimeSpan.FromDays(30);

    public const int BatchSize = 2000;

    private readonly IPlatformSignInCleanupRepository _rows;

    private readonly TimeProvider _timeProvider;

    public PlatformSignInCleanupClock(IPlatformSignInCleanupRepository rows, TimeProvider timeProvider)
    {
        _rows = rows ?? throw new ArgumentNullException(nameof(rows));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        int removed = await _rows.RemoveSessionsExpiredBeforeAsync(utcNow - SessionRetention, BatchSize, cancellationToken);
        return removed + await _rows.RemoveLinksExpiredBeforeAsync(utcNow - LinkRetention, BatchSize, cancellationToken);
    }
}
