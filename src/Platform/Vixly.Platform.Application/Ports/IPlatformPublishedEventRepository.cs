using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformPublishedEventRepository.cs
//
// PURPOSE:
// Stores and reads the public event feed (Slice 9, D-K3).
//
// RESPONSIBILITIES:
// - Add an event once
// - Read events after a cursor
// - Remove events past their retention
//
// IMPORTANT NOTES:
// - Reads return only events older than the settle time, so a cursor never skips a late commit
// =========================================================
public interface IPlatformPublishedEventRepository
{
    void Add(PlatformPublishedEvent item);

    Task<bool> ExistsAsync(Guid outboxMessageId, string type, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformPublishedEvent>> ListAfterAsync(long after, int take, DateTimeOffset createdBeforeUtc, IReadOnlyCollection<string> types, CancellationToken cancellationToken);

    Task<int> DeleteCreatedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken);
}
