using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Vixly.Shared.Hosting.Caching;

// =========================================================
// FILE: VixlyStaleWhileRefresh.cs
//
// PURPOSE:
// Performance plan P-4 and harmonization H-7 (owner, 2026-11-04): a page never waits on another site for an answer it already has.
//
// RESPONSIBILITIES:
// - Serve a kept answer while it is fresh
// - Once it is older, keep serving it and refresh it in the background, one call per key at a time
// - Never serve an answer older than the hard limit: the page then waits for the other site, as before
// - Forget a key at once, and make sure a refresh already under way cannot put the old answer back
//
// IMPORTANT NOTES:
// - Singleton per site; used by the customer portal for its host and brand answers from Operations
// - The same rules as Operations' own copy (OperationsStaleWhileRefresh), which its infrastructure layer keeps because it may not use this library
// - The background call does not carry the request's context (timing, cancellation): it outlives the request
// - What happens when the other site cannot be reached stays with each caller (last good answer, safe defaults)
// =========================================================
public sealed class VixlyStaleWhileRefresh
{
    // The oldest answer a page is ever given while the other site can be reached.
    public static readonly TimeSpan HardLimit = TimeSpan.FromMinutes(10);

    private static readonly TimeSpan RefreshTimeout = TimeSpan.FromSeconds(30);

    private readonly IMemoryCache _cache;

    private readonly TimeProvider _time;

    private readonly ILogger<VixlyStaleWhileRefresh> _logger;

    private readonly ConcurrentDictionary<string, long> _generations = new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, byte> _refreshing = new(StringComparer.Ordinal);

    private readonly object _gate = new();

    public VixlyStaleWhileRefresh(IMemoryCache cache, TimeProvider time, ILogger<VixlyStaleWhileRefresh> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Tests and callers built by hand.
    public VixlyStaleWhileRefresh(IMemoryCache cache)
        : this(cache, TimeProvider.System, NullLogger<VixlyStaleWhileRefresh>.Instance)
    {
    }

    // The kept answer when it is younger than the hard limit; past its fresh time it is refreshed in the background.
    // refresh returns the answer to keep, or null to keep nothing (the kept one then ages out).
    public bool TryGet<T>(string key, TimeSpan fresh, Func<CancellationToken, Task<T?>> refresh, out T value)
        where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(refresh);

        // Read before anything else, in the request: a forget after this point always wins over the refresh.
        long generation = Generation(key);

        if (_cache.TryGetValue(key, out Kept? kept) && kept is { Value: T found })
        {
            TimeSpan age = _time.GetUtcNow() - kept.LoadedUtc;

            if (age < HardLimit)
            {
                if (age >= fresh)
                {
                    StartRefresh(key, refresh, generation);
                }

                value = found;
                return true;
            }
        }

        value = null!;
        return false;
    }

    // Read before a call to the other site; Keep refuses the answer if the key was forgotten meanwhile.
    public long Generation(string key) => _generations.GetValueOrDefault(key);

    public void Keep<T>(string key, T value, long generation)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value);

        lock (_gate)
        {
            if (Generation(key) == generation)
            {
                _cache.Set(key, new Kept(value, _time.GetUtcNow()), HardLimit);
            }
        }
    }

    public void Forget(string key)
    {
        lock (_gate)
        {
            _generations.AddOrUpdate(key, 1, (_, current) => current + 1);
            _cache.Remove(key);
        }
    }

    private void StartRefresh<T>(string key, Func<CancellationToken, Task<T?>> refresh, long generation)
        where T : class
    {
        if (!_refreshing.TryAdd(key, 0))
        {
            return;
        }

        // The request's timing, culture and cancellation stay with the request.
        using (ExecutionContext.SuppressFlow())
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using CancellationTokenSource timeout = new(RefreshTimeout);
                    T? answer = await refresh(timeout.Token);

                    if (answer is not null)
                    {
                        Keep(key, answer, generation);
                    }
                }
                catch (Exception exception)
                {
                    // The kept answer stays until the hard limit; the next page tries again.
                    _logger.LogWarning(exception, "A background refresh from the other site failed; the kept answer is used for now.");
                }
                finally
                {
                    _refreshing.TryRemove(key, out _);
                }
            });
        }
    }

    private sealed record Kept(object Value, DateTimeOffset LoadedUtc);
}
