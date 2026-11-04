using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: IPlatformJobQueries.cs
//
// PURPOSE:
// Reads the run history for the Jobs page.
//
// RESPONSIBILITIES:
// - The latest run of each job, with its failures in the last day
// - The runs, newest first, by job and failures
//
// IMPORTANT NOTES:
// - Read only, without tracking
// =========================================================
public interface IPlatformJobQueries
{
    Task<IReadOnlyList<PlatformJobSummaryItem>> ListLatestAsync(DateTimeOffset failuresSinceUtc, CancellationToken cancellationToken);

    Task<PagedResult<PlatformJobRunItem>> ListRunsAsync(string? job, bool failedOnly, PagedRequest page, CancellationToken cancellationToken);
}
