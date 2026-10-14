using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformOutboxRepository.cs
//
// PURPOSE:
// Stores outbox messages and leases due ones to a dispatcher
// (D-T17).
//
// RESPONSIBILITIES:
// - Add a message to the current unit of work
// - Lease the next due messages, oldest first, one per stream
// - Load leased messages for handling
//
// IMPORTANT NOTES:
// - LeaseDueAsync commits on its own: a lease must hold even if
//   handling fails
// - A stream with an earlier unprocessed message is skipped, so
//   messages of one tenant are handled in order
// =========================================================
public interface IPlatformOutboxRepository
{
    void Add(PlatformOutboxMessage message);

    Task<IReadOnlyList<Guid>> LeaseDueAsync(
        int maxCount,
        DateTimeOffset utcNow,
        DateTimeOffset leasedUntilUtc,
        CancellationToken cancellationToken);

    Task<PlatformOutboxMessage?> GetAsync(Guid messageId, CancellationToken cancellationToken);
}
