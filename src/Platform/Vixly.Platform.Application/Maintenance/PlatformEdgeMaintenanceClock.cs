namespace Vixly.Platform.Application.Maintenance;

// =========================================================
// FILE: PlatformEdgeMaintenanceClock.cs
//
// PURPOSE:
// The hourly check: sends the maintenance windows to the Cloudflare Worker again (edge_sync on the Jobs page).
//
// RESPONSIBILITIES:
// - Send the list; count 1 when the Worker took it, 0 when there is no Worker or it already had it
//
// IMPORTANT NOTES:
// - A failure throws, so the Jobs page shows it
// =========================================================
public sealed class PlatformEdgeMaintenanceClock
{
    private readonly PlatformEdgeMaintenance _edge;

    public PlatformEdgeMaintenanceClock(PlatformEdgeMaintenance edge)
    {
        _edge = edge ?? throw new ArgumentNullException(nameof(edge));
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        return await _edge.SendAsync(cancellationToken) == PlatformEdgeSendResult.Sent ? 1 : 0;
    }
}
