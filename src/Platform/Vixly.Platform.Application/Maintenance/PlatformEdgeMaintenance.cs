using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Maintenance;

namespace Vixly.Platform.Application.Maintenance;

// =========================================================
// FILE: PlatformEdgeMaintenance.cs
//
// PURPOSE:
// Tells the Cloudflare Worker which maintenance windows are open (Worker design, section 2; owner 2026-10-28).
//
// RESPONSIBILITIES:
// - Read the windows not cancelled and not finished, and send them all, signed
//
// IMPORTANT NOTES:
// - Run by the outbox after every publish, cancel and End now, and every hour by the clock (edge_sync)
// - The list is read when it is sent, so a late retry always sends the current windows
// - All messages share one stream, so they go in order
// =========================================================
public sealed class PlatformEdgeMaintenance
{
    public static readonly Guid StreamId = new("6d1f3a8e-2b7c-4e59-9a0d-5c3e7f1b2a64");

    private readonly IPlatformMaintenanceRepository _store;

    private readonly IPlatformEdgeClient _edge;

    private readonly TimeProvider _timeProvider;

    public PlatformEdgeMaintenance(IPlatformMaintenanceRepository store, IPlatformEdgeClient edge, TimeProvider timeProvider)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _edge = edge ?? throw new ArgumentNullException(nameof(edge));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PlatformEdgeSendResult> SendAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        List<PlatformEdgeWindow> windows = new();

        foreach (PlatformMaintenanceWindow window in await _store.ListOpenAsync(utcNow, cancellationToken))
        {
            if (window.StatusAt(utcNow) is PlatformMaintenanceStatus.Cancelled or PlatformMaintenanceStatus.Finished)
            {
                continue;
            }

            Dictionary<string, string> messages = new(StringComparer.Ordinal);
            Add(messages, "en", window.MessageEn);
            Add(messages, "fr", window.MessageFr);
            Add(messages, "es", window.MessageEs);
            windows.Add(new PlatformEdgeWindow(window.Id, window.StartsUtc, window.EndsUtc, PlatformMaintenanceSiteCodes.Of(window.Sites), messages, window.PassHash));
        }

        return await _edge.SendMaintenanceAsync(new PlatformEdgeWindowList(windows), cancellationToken);
    }

    private static void Add(Dictionary<string, string> messages, string language, string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            messages[language] = text;
        }
    }
}
