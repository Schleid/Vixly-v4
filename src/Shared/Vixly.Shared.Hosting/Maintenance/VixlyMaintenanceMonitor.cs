using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vixly.Shared.Hosting.Maintenance;

// =========================================================
// FILE: VixlyMaintenanceMonitor.cs
//
// PURPOSE:
// Keeps each site's maintenance windows for a minute, so no request waits on Platform.
//
// RESPONSIBILITIES:
// - Answer the running and announced window of a site from memory
// - Refresh in the background when the copy is older than the refresh time
//
// IMPORTANT NOTES:
// - Singleton
// - The first request after start sees no window; the refresh it starts fills the copy
// - Platform unreachable: the last copy stays (fail open, never closes a site by mistake)
// =========================================================
public sealed class VixlyMaintenanceMonitor
{
    private readonly IServiceScopeFactory _scopes;

    private readonly VixlyMaintenanceOptions _options;

    private readonly TimeProvider _timeProvider;

    private readonly ILogger<VixlyMaintenanceMonitor> _logger;

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Entry> _sites = new(StringComparer.Ordinal);

    public VixlyMaintenanceMonitor(IServiceScopeFactory scopes, IOptions<VixlyMaintenanceOptions> options, TimeProvider timeProvider, ILogger<VixlyMaintenanceMonitor> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public VixlyMaintenanceWindow? Running(string site)
    {
        DateTimeOffset utcNow = UtcNow;
        return Windows(site).FirstOrDefault(window => window.IsRunning(utcNow));
    }

    public VixlyMaintenanceWindow? Announced(string site)
    {
        DateTimeOffset utcNow = UtcNow;
        return Windows(site).Where(window => window.IsAnnounced(utcNow)).OrderBy(window => window.StartsUtc).FirstOrDefault();
    }

    public async Task<DateTimeOffset?> CheckPassAsync(string site, string token, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IVixlyMaintenanceSource>().CheckPassAsync(site, token, cancellationToken);
    }

    // Only for tests and the first start: fills the copy now.
    public async Task RefreshAsync(string site, CancellationToken cancellationToken)
    {
        Entry entry = _sites.GetOrAdd(site, _ => new Entry());
        await RefreshAsync(site, entry, cancellationToken);
    }

    private IReadOnlyList<VixlyMaintenanceWindow> Windows(string site)
    {
        if (!_options.Enabled)
        {
            return Array.Empty<VixlyMaintenanceWindow>();
        }

        Entry entry = _sites.GetOrAdd(site, _ => new Entry());

        if (UtcNow - entry.ReadUtc >= TimeSpan.FromSeconds(Math.Max(5, _options.RefreshSeconds)) && Interlocked.CompareExchange(ref entry.Refreshing, 1, 0) == 0)
        {
            _ = Task.Run(() => RefreshAsync(site, entry, CancellationToken.None));
        }

        return entry.Windows;
    }

    private async Task RefreshAsync(string site, Entry entry, CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
            IReadOnlyList<VixlyMaintenanceWindow>? windows = await scope.ServiceProvider.GetRequiredService<IVixlyMaintenanceSource>().ReadAsync(site, cancellationToken);

            if (windows is not null)
            {
                entry.Windows = windows;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Maintenance windows of {Site} could not be read; the last copy stays.", site);
        }
        finally
        {
            entry.ReadUtc = UtcNow;
            Volatile.Write(ref entry.Refreshing, 0);
        }
    }

    private sealed class Entry
    {
        public int Refreshing;

        public volatile IReadOnlyList<VixlyMaintenanceWindow> Windows = Array.Empty<VixlyMaintenanceWindow>();

        public DateTimeOffset ReadUtc = DateTimeOffset.MinValue;
    }
}
