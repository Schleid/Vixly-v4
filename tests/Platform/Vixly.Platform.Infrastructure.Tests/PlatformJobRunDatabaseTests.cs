using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Jobs;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Jobs;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Queries;
using Vixly.Platform.Infrastructure.Persistence.Repositories;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformJobRunDatabaseTests.cs
//
// PURPOSE:
// Job runs are recorded, read for the Jobs page and pruned after 90 days (owner, 2026-10-23).
//
// RESPONSIBILITIES:
// - The recorder writes a run with its failure
// - The latest run, the last success and recent failures per job
// - Runs filtered by job and failures, newest first
// - Runs past 90 days removed, recent ones kept
//
// IMPORTANT NOTES:
// - Job names are unique to each test run, so other rows never mix in
// - Dated in 2020 so the prune touches no other test's rows
// =========================================================
public sealed class PlatformJobRunDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2020, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PlatformDatabaseFixture _database;

    public PlatformJobRunDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task Runs_AreRecorded_Summarised_Filtered_AndPruned()
    {
        string job = "test_" + Guid.NewGuid().ToString("N")[..12];
        PlatformJobRun old = Run(job, Now.AddDays(-100), 3, null);
        PlatformJobRun good = Run(job, Now.AddHours(-30), 2, null);
        PlatformJobRun bad = Run(job, Now.AddHours(-2), 0, "TimeoutException");

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.JobRuns.AddRange(old, good, bad);
            await context.SaveChangesAsync();
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            await new PlatformJobRecorder(new PlatformJobRunRepository(context), new PlatformUnitOfWork(context), new FixedTimeProvider(Now))
                .RecordAsync(job, Now.AddMinutes(-1), 5, null, CancellationToken.None);
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformJobQueries queries = new(context);
            PlatformJobSummaryItem summary = (await queries.ListLatestAsync(Now.AddHours(-24), CancellationToken.None)).Single(item => item.Job == job);

            Assert.Equal(5, summary.Latest!.Handled);
            Assert.Equal(0, summary.Latest.Failed);
            Assert.Equal(Now.AddMinutes(-1), summary.LastSucceededUtc);
            Assert.Equal(1, summary.FailedRecently);

            PagedResult<PlatformJobRunItem> failures = await queries.ListRunsAsync(job, true, PagedRequest.Create(1, 25), CancellationToken.None);
            Assert.Equal("TimeoutException", Assert.Single(failures.Items).ErrorCode);

            PagedResult<PlatformJobRunItem> all = await queries.ListRunsAsync(job, false, PagedRequest.Create(1, 25), CancellationToken.None);
            Assert.Equal(4, all.TotalCount);
            Assert.Equal(5, all.Items[0].Handled);
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            Assert.True(await new PlatformJobRunClock(new PlatformJobRunRepository(context), new FixedTimeProvider(Now)).RunAsync(CancellationToken.None) >= 1);
        }

        await using PlatformDbContext check = _database.NewContext();
        Assert.False(await check.JobRuns.AnyAsync(item => item.Id == old.Id));
        Assert.True(await check.JobRuns.AnyAsync(item => item.Id == good.Id));
    }

    private static PlatformJobRun Run(string job, DateTimeOffset startedUtc, int handled, string? errorCode)
    {
        PlatformJobRun run = PlatformJobRun.Start(job, startedUtc);
        run.Finish(handled, errorCode is null ? 0 : 1, errorCode, startedUtc.AddSeconds(2));

        return run;
    }
}
