using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Jobs;

namespace Vixly.Platform.Application.Jobs;

// =========================================================
// FILE: PlatformJobRunClock.cs
//
// PURPOSE:
// Removes job runs past their 90 days.
//
// RESPONSIBILITIES:
// - Delete old run rows
//
// IMPORTANT NOTES:
// - Run by the subscription clock with the other steps
// =========================================================
public sealed class PlatformJobRunClock
{
    private readonly IPlatformJobRunRepository _runs;

    private readonly TimeProvider _timeProvider;

    public PlatformJobRunClock(IPlatformJobRunRepository runs, TimeProvider timeProvider)
    {
        _runs = runs ?? throw new ArgumentNullException(nameof(runs));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Task<int> RunAsync(CancellationToken cancellationToken)
    {
        return _runs.DeleteCreatedBeforeAsync(_timeProvider.GetUtcNow() - PlatformJobRun.Retention, cancellationToken);
    }
}
