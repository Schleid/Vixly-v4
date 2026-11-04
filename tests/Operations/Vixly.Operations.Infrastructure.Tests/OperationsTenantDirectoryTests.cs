using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Infrastructure.Platform;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsTenantDirectoryTests.cs
//
// PURPOSE:
// Proves the host-name cache keeps a known tenant up while Platform hiccups (owner, 2026-10-20: "please try again" on a running system).
//
// RESPONSIBILITIES:
// - A found tenant is served from its last good answer when Platform fails
// - An unknown tenant is asked again after 10 seconds, not a minute
// - Forget drops the last good answer too
// - P-4: a stale answer is served at once and refreshed in the background; never past 10 minutes; a forget wins over a refresh under way
//
// IMPORTANT NOTES:
// - No network: a scripted Platform
// =========================================================
public sealed class OperationsTenantDirectoryTests
{
    private static readonly OperationsPortalAnswer Live = new(true, Guid.NewGuid(), "live", "full", "andes-courier", "es", "none");

    private static readonly OperationsPortalAnswer Unknown = new(false, null, null, "none", null, null, "none");

    [Fact]
    public async Task PlatformDown_AKnownTenantKeepsItsLastGoodAnswer_AnUnknownOneIsStillUnavailable()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        ScriptedPlatform platform = new();
        OperationsTenantDirectory directory = new(cache, platform);

        platform.Answer = Live;
        Assert.Same(Live, await directory.FindAsync("andes-courier", CancellationToken.None));

        // The minute is over and Platform is down: the tenant stays as it was.
        cache.Remove("ops.portal:andes-courier");
        platform.Answer = null;
        Assert.Same(Live, await directory.FindAsync("andes-courier", CancellationToken.None));
        await Assert.ThrowsAsync<OperationsPlatformUnavailableException>(() => directory.FindAsync("never-seen", CancellationToken.None));

        // Forgotten (suspended, renamed): nothing old is served any more.
        directory.Forget(Live.TenantId!.Value);
        await Assert.ThrowsAsync<OperationsPlatformUnavailableException>(() => directory.FindAsync("andes-courier", CancellationToken.None));
    }

    [Fact]
    public async Task NotFound_IsKeptTenSecondsOnly()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        ScriptedPlatform platform = new() { Answer = Unknown };
        OperationsTenantDirectory directory = new(cache, platform);

        Assert.False((await directory.FindAsync("new-co", CancellationToken.None)).Found);
        Assert.Equal(1, platform.Calls);
        Assert.Equal(TimeSpan.FromSeconds(10), OperationsTenantDirectory.NotFoundCacheTime);
        Assert.False((await directory.FindAsync("new-co", CancellationToken.None)).Found);
        Assert.Equal(1, platform.Calls);
    }

    [Fact]
    public async Task StaleAnswer_IsServedAtOnce_AndRefreshedInTheBackground_NeverPastTheHardLimit()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        ManualClock clock = new();
        ScriptedPlatform platform = new() { Answer = Live };
        OperationsTenantDirectory directory = new(cache, platform, new OperationsStaleWhileRefresh(cache, clock, NullLogger<OperationsStaleWhileRefresh>.Instance));

        Assert.Same(Live, await directory.FindAsync("andes-courier", CancellationToken.None));

        // A minute later Platform is slow: the page still gets the kept answer at once, and one call refreshes it.
        OperationsPortalAnswer renamed = Live with { DefaultLanguage = "fr" };
        clock.Advance(TimeSpan.FromSeconds(61));
        platform.Answer = renamed;
        platform.Hold = new TaskCompletionSource();
        Assert.Same(Live, await directory.FindAsync("andes-courier", CancellationToken.None));
        await UntilAsync(() => platform.Calls == 2);
        Assert.Same(Live, await directory.FindAsync("andes-courier", CancellationToken.None));
        Assert.Equal(2, platform.Calls);

        platform.Hold.SetResult();
        Assert.Same(renamed, await EventuallyAsync(directory, renamed));
        Assert.Equal(2, platform.Calls);

        // Past the hard limit the page waits for Platform, as before P-4.
        OperationsPortalAnswer later = Live with { DefaultLanguage = "en" };
        platform.Answer = later;
        clock.Advance(OperationsStaleWhileRefresh.HardLimit);
        Assert.Same(later, await directory.FindAsync("andes-courier", CancellationToken.None));
        Assert.Equal(3, platform.Calls);
    }

    [Fact]
    public async Task Forget_WinsOverARefreshUnderWay()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        ManualClock clock = new();
        ScriptedPlatform platform = new() { Answer = Live };
        OperationsTenantDirectory directory = new(cache, platform, new OperationsStaleWhileRefresh(cache, clock, NullLogger<OperationsStaleWhileRefresh>.Instance));
        await directory.FindAsync("andes-courier", CancellationToken.None);

        // The refresh leaves with the old answer; meanwhile Control suspends the company and the event feed forgets it.
        clock.Advance(TimeSpan.FromSeconds(61));
        platform.Hold = new TaskCompletionSource();
        Assert.Same(Live, await directory.FindAsync("andes-courier", CancellationToken.None));
        directory.Forget(Live.TenantId!.Value);
        platform.Hold.SetResult();
        await platform.Done.Task;

        // The old answer did not come back: the next page asks Platform and gets the suspension.
        OperationsPortalAnswer suspended = Live with { Status = "suspended" };
        platform.Hold = null;
        platform.Answer = suspended;
        Assert.Same(suspended, await directory.FindAsync("andes-courier", CancellationToken.None));
        Assert.Equal(3, platform.Calls);
    }

    [Fact]
    public async Task ASecondForget_StillReachesANameAnsweredMeanwhile()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        ScriptedPlatform platform = new() { Answer = Live };
        OperationsTenantDirectory directory = new(cache, platform);
        await directory.FindAsync("andes-courier", CancellationToken.None);
        directory.Forget(Live.TenantId!.Value);

        // A page asks Platform again; while it waits, Control suspends the company and the feed forgets it once more.
        platform.Hold = new TaskCompletionSource();
        Task<OperationsPortalAnswer> page = directory.FindAsync("andes-courier", CancellationToken.None);
        directory.Forget(Live.TenantId!.Value);
        platform.Hold.SetResult();
        Assert.Same(Live, await page);

        // The answer that page got is not kept: the next page asks Platform.
        OperationsPortalAnswer suspended = Live with { Status = "suspended" };
        platform.Hold = null;
        platform.Answer = suspended;
        Assert.Same(suspended, await directory.FindAsync("andes-courier", CancellationToken.None));
        Assert.Equal(3, platform.Calls);
    }

    private static async Task UntilAsync(Func<bool> condition)
    {
        for (int attempt = 0; attempt < 200 && !condition(); attempt++)
        {
            await Task.Delay(10);
        }
    }

    private static async Task<OperationsPortalAnswer> EventuallyAsync(OperationsTenantDirectory directory, OperationsPortalAnswer expected)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            OperationsPortalAnswer answer = await directory.FindAsync("andes-courier", CancellationToken.None);

            if (ReferenceEquals(answer, expected))
            {
                return answer;
            }

            await Task.Delay(10);
        }

        return await directory.FindAsync("andes-courier", CancellationToken.None);
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    // Answers what it is told; null: Platform cannot be reached. Hold: Platform answers only once released.
    private sealed class ScriptedPlatform : IOperationsPlatformClient
    {
        public OperationsPortalAnswer? Answer { get; set; }

        public TaskCompletionSource? Hold { get; set; }

        // Set once a held call has answered and been kept or dropped.
        public TaskCompletionSource Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Calls { get; private set; }

        public async Task<OperationsPortalAnswer> GetPortalAsync(string slug, CancellationToken cancellationToken)
        {
            Calls++;
            OperationsPortalAnswer? answer = Answer;

            if (Hold is { } hold)
            {
                await hold.Task;
                _ = Task.Delay(50).ContinueWith(_ => Done.TrySetResult(), TaskScheduler.Default);
            }

            return answer ?? throw new OperationsPlatformUnavailableException("down");
        }

        public Task<OperationsPlatformEventPage> ReadEventsAsync(long after, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<OperationsTenantAccount?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
