using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Jobs;
using Vixly.Operations.Domain.Messaging;
using Vixly.Operations.Domain.Platform;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakeStores.cs
//
// PURPOSE:
// In-memory outbox, job runs, feed position and unit of work.
//
// RESPONSIBILITIES:
// - Keep what use cases add
// - Count saves and discards
//
// IMPORTANT NOTES:
// - Tests only. One class implements every port so a test sees one store
// =========================================================
internal sealed class FakeStores : IOperationsOutboxRepository, IOperationsJobRunRepository, IOperationsFeedPositionRepository, IOperationsUnitOfWork
{
    public List<OperationsOutboxMessage> Outbox { get; } = new();

    public List<OperationsJobRun> Runs { get; } = new();

    public List<OperationsPlatformFeedPosition> Positions { get; } = new();

    public int Saves { get; private set; }

    public int Discards { get; private set; }

    public void Add(OperationsOutboxMessage message) => Outbox.Add(message);

    public Task<IReadOnlyList<Guid>> LeaseDueAsync(int max, DateTimeOffset utcNow, DateTimeOffset leasedUntilUtc, CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> due = Outbox.Where(message => message.ProcessedUtc is null && message.NextAttemptUtc <= utcNow).Take(max).Select(message => message.Id).ToList();
        return Task.FromResult(due);
    }

    public Task<OperationsOutboxMessage?> GetAsync(Guid messageId, CancellationToken cancellationToken) => Task.FromResult(Outbox.FirstOrDefault(message => message.Id == messageId));

    public void Add(OperationsJobRun run) => Runs.Add(run);

    public Task<OperationsPlatformFeedPosition?> FindAsync(string feed, CancellationToken cancellationToken) => Task.FromResult(Positions.FirstOrDefault(position => position.Feed == feed));

    public void Add(OperationsPlatformFeedPosition position) => Positions.Add(position);

    public Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
    {
        Saves++;
        return Task.FromResult(true);
    }

    public void DiscardChanges() => Discards++;
}
