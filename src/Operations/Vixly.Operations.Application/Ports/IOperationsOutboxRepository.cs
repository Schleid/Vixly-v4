using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsOutboxRepository.cs
//
// PURPOSE:
// Stores outbox messages and leases the due ones.
//
// RESPONSIBILITIES:
// - Add messages; lease due messages in stream order; load one
//
// IMPORTANT NOTES:
// - Leasing is atomic so two hosts never take the same message
// =========================================================
public interface IOperationsOutboxRepository
{
    void Add(OperationsOutboxMessage message);

    Task<IReadOnlyList<Guid>> LeaseDueAsync(int max, DateTimeOffset utcNow, DateTimeOffset leasedUntilUtc, CancellationToken cancellationToken);

    Task<OperationsOutboxMessage?> GetAsync(Guid messageId, CancellationToken cancellationToken);
}
