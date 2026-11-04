using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence;

// =========================================================
// FILE: PlatformIdleTimesReader.cs
//
// PURPOSE:
// Reads Control's inactivity times from the security settings, kept one minute (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Read the two numbers without tracking the row
// - Keep them one minute; forget them on a change
//
// IMPORTANT NOTES:
// - No row: the defaults
// =========================================================
internal sealed class PlatformIdleTimesReader : IPlatformIdleTimesReader
{
    private const string Key = "vixly.platform.idle";

    private static readonly TimeSpan KeptFor = TimeSpan.FromMinutes(1);

    private readonly PlatformDbContext _context;

    private readonly IMemoryCache _cache;

    public PlatformIdleTimesReader(PlatformDbContext context, IMemoryCache cache)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<PlatformIdleTimes> GetAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(Key, out PlatformIdleTimes? kept) && kept is not null)
        {
            return kept;
        }

        var row = await _context.Set<PlatformSecuritySettings>().AsNoTracking()
            .Where(settings => settings.Key == PlatformSecuritySettings.SingletonKey)
            .Select(settings => new { settings.IdleMinutes, settings.IdleWarnMinutes })
            .FirstOrDefaultAsync(cancellationToken);
        PlatformIdleTimes times = row is null
            ? PlatformIdleTimes.Default
            : new PlatformIdleTimes(TimeSpan.FromMinutes(row.IdleMinutes), TimeSpan.FromMinutes(row.IdleWarnMinutes));
        _cache.Set(Key, times, KeptFor);
        return times;
    }

    public void Forget() => _cache.Remove(Key);
}
