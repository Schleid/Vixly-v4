using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Platform;

// =========================================================
// FILE: OperationsTenantDirectory.cs
//
// PURPOSE:
// Platform answers about host names, cached for 60 seconds (Stage 3, Slice 0, D-F2), then refreshed in the background (P-4).
//
// RESPONSIBILITIES:
// - Answer from the cache, else ask Platform
// - Forget every cached slug of a tenant at once
// - P-4 (owner, 2026-11-04): after 60 seconds the kept answer is still served while one background call refreshes it,
//   never beyond 10 minutes (OperationsStaleWhileRefresh.HardLimit); a forget wins over a refresh under way for every name this
//   process has seen for the tenant (the names are kept for that reason)
//
// IMPORTANT NOTES:
// - Singleton: the cache is shared by every request of this process
// - "Not found" is cached for 10 seconds only, so an unknown name cannot hammer Platform and a tenant created or seeded a moment ago appears at once
// - Platform unreachable is never cached; the last good answer is used instead for up to 24 hours, and a slow or restarting Platform
//   no longer shows the "please try again" page to a known tenant (owner, 2026-10-20)
// =========================================================
public sealed class OperationsTenantDirectory : IOperationsTenantDirectory
{
    public static readonly TimeSpan CacheTime = TimeSpan.FromSeconds(60);

    public static readonly TimeSpan NotFoundCacheTime = TimeSpan.FromSeconds(10);

    public static readonly TimeSpan LastGoodTime = TimeSpan.FromHours(24);

    private readonly IMemoryCache _cache;

    private readonly IOperationsPlatformClient _platform;

    private readonly OperationsStaleWhileRefresh _fresh;

    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _slugsByTenant = new();

    // A forget and the keeping of an answer never interleave, so a forgotten tenant cannot come back from a refresh under way.
    private readonly object _gate = new();

    public OperationsTenantDirectory(IMemoryCache cache, IOperationsPlatformClient platform, OperationsStaleWhileRefresh fresh)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _fresh = fresh ?? throw new ArgumentNullException(nameof(fresh));
    }

    // Tests and callers built by hand.
    public OperationsTenantDirectory(IMemoryCache cache, IOperationsPlatformClient platform)
        : this(cache, platform, new OperationsStaleWhileRefresh(cache))
    {
    }

    public async Task<OperationsPortalAnswer> FindAsync(string slug, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        string key = Key(slug);

        // Read now, in the request: a forget after this point wins, even over a refresh not yet started.
        long generation = _fresh.Generation(key);

        if (_fresh.TryGet<OperationsPortalAnswer>(key, CacheTime, token => RefreshAsync(slug, generation, token), out OperationsPortalAnswer kept))
        {
            return kept;
        }

        if (_cache.TryGetValue(NotFoundKey(slug), out OperationsPortalAnswer? missing) && missing is not null)
        {
            return missing;
        }

        OperationsPortalAnswer answer;

        try
        {
            answer = await _platform.GetPortalAsync(slug, cancellationToken);
        }
        catch (OperationsPlatformUnavailableException) when (_cache.TryGetValue(LastGoodKey(slug), out OperationsPortalAnswer? lastGood) && lastGood is not null)
        {
            // Platform is down or slow for a moment: the tenant stays as it last was.
            return lastGood;
        }

        Remember(slug, answer, generation);
        return answer;
    }

    public void Forget(Guid tenantId)
    {
        // The names stay on record (review, 2026-11-04): a later forget must still reach a name answered meanwhile.
        if (_slugsByTenant.TryGetValue(tenantId, out ConcurrentDictionary<string, byte>? slugs))
        {
            foreach (string slug in slugs.Keys)
            {
                lock (_gate)
                {
                    _fresh.Forget(Key(slug));
                    _cache.Remove(LastGoodKey(slug));
                    _cache.Remove(NotFoundKey(slug));
                }
            }
        }
    }

    // The background call (P-4): it keeps the answer itself, under the same rules as a page's call.
    private async Task<OperationsPortalAnswer?> RefreshAsync(string slug, long generation, CancellationToken cancellationToken)
    {
        Remember(slug, await _platform.GetPortalAsync(slug, cancellationToken), generation);
        return null;
    }

    private void Remember(string slug, OperationsPortalAnswer answer, long generation)
    {
        string key = Key(slug);

        lock (_gate)
        {
            // Forgotten while Platform answered (suspended, renamed): the answer is dropped and the next page asks again.
            if (_fresh.Generation(key) != generation)
            {
                return;
            }

            if (answer.Found)
            {
                _fresh.Keep(key, answer, generation);
                _cache.Set(LastGoodKey(slug), answer, LastGoodTime);
                _cache.Remove(NotFoundKey(slug));
            }
            else
            {
                // Gone: nothing old is served any more.
                _fresh.Forget(key);
                _cache.Remove(LastGoodKey(slug));
                _cache.Set(NotFoundKey(slug), answer, NotFoundCacheTime);
            }

            if (answer.TenantId is { } tenantId)
            {
                _slugsByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))[slug] = 0;
            }
        }
    }

    private static string Key(string slug) => "ops.portal:" + slug;

    private static string NotFoundKey(string slug) => "ops.portal.none:" + slug;

    private static string LastGoodKey(string slug) => "ops.portal.good:" + slug;
}
