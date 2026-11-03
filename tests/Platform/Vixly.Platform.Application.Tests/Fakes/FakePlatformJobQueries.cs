using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakePlatformJobQueries.cs
//
// PURPOSE:
// In-memory run history for the Jobs use case tests.
//
// RESPONSIBILITIES:
// - Answer the latest runs and the runs page from a list
// - Remember the last filter
//
// IMPORTANT NOTES:
// - Test double only
// =========================================================
internal sealed class FakePlatformJobQueries : IPlatformJobQueries
{
    public List<PlatformJobSummaryItem> Latest { get; } = new();

    public (string? Job, bool FailedOnly)? LastFilter { get; private set; }

    public Task<IReadOnlyList<PlatformJobSummaryItem>> ListLatestAsync(DateTimeOffset failuresSinceUtc, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformJobSummaryItem>>(Latest);
    }

    public Task<PagedResult<PlatformJobRunItem>> ListRunsAsync(string? job, bool failedOnly, PagedRequest page, CancellationToken cancellationToken)
    {
        LastFilter = (job, failedOnly);

        return Task.FromResult(new PagedResult<PlatformJobRunItem>(Array.Empty<PlatformJobRunItem>(), page, 0));
    }
}
