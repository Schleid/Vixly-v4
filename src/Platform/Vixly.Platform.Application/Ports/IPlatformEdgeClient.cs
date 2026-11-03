using Vixly.Platform.Application.Maintenance;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformEdgeClient.cs
//
// PURPOSE:
// Sends the maintenance windows to the Cloudflare Worker in front of the sites (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Send the whole list of open windows, signed
//
// IMPORTANT NOTES:
// - Not configured (no address or secret): NotConfigured, nothing sent, never an error
// - A refusal or an unreachable Worker throws, so the outbox tries again
// =========================================================
public interface IPlatformEdgeClient
{
    Task<PlatformEdgeSendResult> SendMaintenanceAsync(PlatformEdgeWindowList list, CancellationToken cancellationToken);
}
