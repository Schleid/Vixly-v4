using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Vixly.Shared.Hosting.Caching;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: VixlyStaleWhileRefreshTests.cs
//
// PURPOSE:
// Proves the shared stale-while-refresh helper the customer portal uses for its host and brand (harmonization H-7, owner 2026-11-04).
//
// RESPONSIBILITIES:
// - A fresh answer is served as is; an older one is served at once and refreshed by one background call
// - Past the hard limit nothing is served: the page waits, as before
// - A forget wins over a refresh already under way
//
// IMPORTANT NOTES:
// - A clock moved by hand; the refresh is held until released
// =========================================================
public sealed class VixlyStaleWhileRefreshTests
{
    [Fact]
    public async Task StaleIsServed_RefreshedOnce_NeverPastTheLimit_AndAForgetWins()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        Clock clock = new();
        VixlyStaleWhileRefresh fresh = new(cache, clock, NullLogger<VixlyStaleWhileRefresh>.Instance);
        TimeSpan minute = TimeSpan.FromSeconds(60);
        fresh.Keep("brand", "old", fresh.Generation("brand"));

        // Fresh: no call at all.
        int calls = 0;
        TaskCompletionSource hold = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<CancellationToken, Task<string?>> refresh = async _ =>
        {
            Interlocked.Increment(ref calls);
            await hold.Task;
            return "new";
        };
        Assert.True(fresh.TryGet("brand", minute, refresh, out string first));
        Assert.Equal(("old", 0), (first, calls));

        // Older than its fresh time: served at once, refreshed by one call however many pages ask.
        clock.Advance(TimeSpan.FromSeconds(61));
        Assert.True(fresh.TryGet("brand", minute, refresh, out string stale));
        Assert.True(fresh.TryGet("brand", minute, refresh, out _));
        Assert.Equal("old", stale);
        await UntilAsync(() => Volatile.Read(ref calls) == 1);
        hold.SetResult();
        await UntilAsync(() => fresh.TryGet("brand", minute, refresh, out string now) && now == "new");
        Assert.Equal(1, Volatile.Read(ref calls));

        // Past the hard limit: nothing is served.
        clock.Advance(VixlyStaleWhileRefresh.HardLimit);
        Assert.False(fresh.TryGet("brand", minute, refresh, out _));

        // A forget during a refresh: the refreshed answer is dropped.
        fresh.Keep("host", "active", fresh.Generation("host"));
        clock.Advance(TimeSpan.FromSeconds(61));
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool answered = false;
        Assert.True(fresh.TryGet<string>("host", minute, async _ =>
        {
            await release.Task;
            answered = true;
            return "active";
        }, out _));
        fresh.Forget("host");
        release.SetResult();
        await UntilAsync(() => Volatile.Read(ref answered));
        await Task.Delay(50);
        Assert.False(fresh.TryGet("host", minute, refresh, out _));
    }

    private static async Task UntilAsync(Func<bool> condition)
    {
        for (int attempt = 0; attempt < 300 && !condition(); attempt++)
        {
            await Task.Delay(10);
        }

        Assert.True(condition());
    }

    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
