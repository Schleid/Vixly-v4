using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Jobs;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Persistence.Queries;

// =========================================================
// FILE: PlatformJobQueries.cs
//
// PURPOSE:
// Reads the run history for the Jobs page.
//
// RESPONSIBILITIES:
// - The latest run per job, its last success and recent failures
// - The runs, newest first
//
// IMPORTANT NOTES:
// - Read without tracking
// =========================================================
internal sealed class PlatformJobQueries : IPlatformJobQueries
{
    private readonly PlatformDbContext _context;

    public PlatformJobQueries(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<PlatformJobSummaryItem>> ListLatestAsync(DateTimeOffset failuresSinceUtc, CancellationToken cancellationToken)
    {
        IQueryable<PlatformJobRun> runs = _context.JobRuns.AsNoTracking();

        List<PlatformJobRun> latest = await runs
            .GroupBy(run => run.Job)
            .Select(group => group.OrderByDescending(run => run.CreatedUtc).First())
            .ToListAsync(cancellationToken);

        Dictionary<string, DateTimeOffset> succeeded = await runs
            .Where(run => run.Failed == 0)
            .GroupBy(run => run.Job)
            .Select(group => new { Job = group.Key, Last = group.Max(run => run.CreatedUtc) })
            .ToDictionaryAsync(row => row.Job, row => row.Last, StringComparer.Ordinal, cancellationToken);

        Dictionary<string, int> failures = await runs
            .Where(run => run.Failed > 0 && run.CreatedUtc >= failuresSinceUtc)
            .GroupBy(run => run.Job)
            .Select(group => new { Job = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Job, row => row.Count, StringComparer.Ordinal, cancellationToken);

        return latest
            .Select(run => new PlatformJobSummaryItem(
                run.Job,
                Item(run),
                succeeded.TryGetValue(run.Job, out DateTimeOffset last) ? last : null,
                failures.GetValueOrDefault(run.Job)))
            .OrderBy(item => item.Job, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<PagedResult<PlatformJobRunItem>> ListRunsAsync(string? job, bool failedOnly, PagedRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        IQueryable<PlatformJobRun> runs = _context.JobRuns.AsNoTracking();

        if (job is not null)
        {
            runs = runs.Where(run => run.Job == job);
        }

        if (failedOnly)
        {
            runs = runs.Where(run => run.Failed > 0);
        }

        int total = await runs.CountAsync(cancellationToken);
        List<PlatformJobRun> rows = await runs
            .OrderByDescending(run => run.CreatedUtc)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformJobRunItem>(rows.Select(Item).ToList(), page, total);
    }

    private static PlatformJobRunItem Item(PlatformJobRun run)
    {
        return new PlatformJobRunItem(run.Id, run.Job, run.CreatedUtc, run.FinishedUtc, run.Handled, run.Failed, run.ErrorCode);
    }
}
