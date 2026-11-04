using Vixly.Platform.Application.Maintenance;
using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformEdgeMaintenanceHandler.cs
//
// PURPOSE:
// Sends the maintenance windows to the Cloudflare Worker after each change (Worker design, section 2).
//
// RESPONSIBILITIES:
// - Send the whole current list
//
// IMPORTANT NOTES:
// - A failure throws: the outbox tries again with back-off until the Worker accepts
// - Without a Worker configured, nothing is sent and the message is done
// =========================================================
public sealed class PlatformEdgeMaintenanceHandler : IPlatformOutboxHandler
{
    private readonly PlatformEdgeMaintenance _edge;

    public PlatformEdgeMaintenanceHandler(PlatformEdgeMaintenance edge)
    {
        _edge = edge ?? throw new ArgumentNullException(nameof(edge));
    }

    public string MessageType => PlatformMessageTypes.EdgeMaintenanceChanged;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        await _edge.SendAsync(cancellationToken);
    }
}
