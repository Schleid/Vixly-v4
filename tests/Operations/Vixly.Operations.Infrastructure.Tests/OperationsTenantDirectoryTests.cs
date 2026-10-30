using Microsoft.Extensions.Caching.Memory;
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

    // Answers what it is told; null: Platform cannot be reached.
    private sealed class ScriptedPlatform : IOperationsPlatformClient
    {
        public OperationsPortalAnswer? Answer { get; set; }

        public int Calls { get; private set; }

        public Task<OperationsPortalAnswer> GetPortalAsync(string slug, CancellationToken cancellationToken)
        {
            Calls++;
            return Answer is { } answer ? Task.FromResult(answer) : throw new OperationsPlatformUnavailableException("down");
        }

        public Task<OperationsPlatformEventPage> ReadEventsAsync(long after, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<OperationsTenantAccount?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
