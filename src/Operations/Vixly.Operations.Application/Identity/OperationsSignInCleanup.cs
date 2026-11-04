using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsSignInCleanup.cs
//
// PURPOSE:
// Keeps the sign-in tables from growing forever (background
// jobs comparison, 2026-10-22: V3 purged its tokens, V4 did not).
//
// RESPONSIBILITIES:
// - Sessions go a year after they expired: they are each
//   person's sign-in history (Activity pages)
// - Challenges and links go 30 days after they expired: an
//   expired one is refused either way
//
// IMPORTANT NOTES:
// - Safe to run twice or after a gap: it only deletes what is
//   past its date
// =========================================================
public sealed class OperationsSignInCleanup
{
    public static readonly TimeSpan SessionRetention = TimeSpan.FromDays(365);

    public static readonly TimeSpan LinkRetention = TimeSpan.FromDays(30);

    public const int BatchSize = 2000;

    private readonly IOperationsSignInCleanupRepository _rows;

    public OperationsSignInCleanup(IOperationsSignInCleanupRepository rows)
    {
        _rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    public async Task<int> RunAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        int removed = await _rows.RemoveSessionsExpiredBeforeAsync(utcNow - SessionRetention, BatchSize, cancellationToken);
        return removed + await _rows.RemoveLinksExpiredBeforeAsync(utcNow - LinkRetention, BatchSize, cancellationToken);
    }
}
