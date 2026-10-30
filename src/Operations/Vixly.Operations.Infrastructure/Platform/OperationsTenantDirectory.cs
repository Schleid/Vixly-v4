using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Platform;

// =========================================================
// FILE: OperationsTenantDirectory.cs
//
// PURPOSE:
// Platform answers about host names, cached for 60 seconds (Stage 3, Slice 0, D-F2).
//
// RESPONSIBILITIES:
// - Answer from the cache, else ask Platform
// - Forget every cached slug of a tenant at once
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

    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _slugsByTenant = new();

    public OperationsTenantDirectory(IMemoryCache cache, IOperationsPlatformClient platform)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
    }

    public async Task<OperationsPortalAnswer> FindAsync(string slug, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        string key = Key(slug);

        if (_cache.TryGetValue(key, out OperationsPortalAnswer? cached) && cached is not null)
        {
            return cached;
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

        _cache.Set(key, answer, answer.Found ? CacheTime : NotFoundCacheTime);

        if (answer.Found)
        {
            _cache.Set(LastGoodKey(slug), answer, LastGoodTime);
        }

        if (answer.TenantId is { } tenantId)
        {
            _slugsByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))[slug] = 0;
        }

        return answer;
    }

    public void Forget(Guid tenantId)
    {
        if (_slugsByTenant.TryRemove(tenantId, out ConcurrentDictionary<string, byte>? slugs))
        {
            foreach (string slug in slugs.Keys)
            {
                _cache.Remove(Key(slug));
                _cache.Remove(LastGoodKey(slug));
            }
        }
    }

    private static string Key(string slug) => "ops.portal:" + slug;

    private static string LastGoodKey(string slug) => "ops.portal.good:" + slug;
}
