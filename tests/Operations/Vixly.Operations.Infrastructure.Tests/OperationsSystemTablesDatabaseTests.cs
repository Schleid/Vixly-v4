using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Messaging;
using Vixly.Operations.Domain.Platform;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Operations.Infrastructure.Persistence.Repositories;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsSystemTablesDatabaseTests.cs
//
// PURPOSE:
// Proves the outbox lease and the feed position against SQL Server (Slice 0, D-F6).
//
// RESPONSIBILITIES:
// - Lease a due message once
// - Keep a stream in order
// - Refuse a stale feed position
//
// IMPORTANT NOTES:
// - Runs against a throwaway SQL Server database
// =========================================================
public sealed class OperationsSystemTablesDatabaseTests : IClassFixture<OperationsDatabaseFixture>
{
    private readonly OperationsDatabaseFixture _database;

    public OperationsSystemTablesDatabaseTests(OperationsDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task Lease_HandsAMessageOutOnce()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        OperationsOutboxMessage message = OperationsOutboxMessage.Create("ops.test.lease.v1", Guid.NewGuid(), Guid.NewGuid(), "{}", now.AddSeconds(-5));
        await Save(message);

        await using OperationsDbContext first = _database.NewSystemContext();
        await using OperationsDbContext second = _database.NewSystemContext();
        IReadOnlyList<Guid> leased = await new OperationsOutboxRepository(first).LeaseDueAsync(50, now, now.AddMinutes(2), CancellationToken.None);
        IReadOnlyList<Guid> again = await new OperationsOutboxRepository(second).LeaseDueAsync(50, now, now.AddMinutes(2), CancellationToken.None);

        Assert.Contains(message.Id, leased);
        Assert.DoesNotContain(message.Id, again);
    }

    [SqlServerFact]
    public async Task Lease_WaitsForTheEarlierMessageOfAStream()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid stream = Guid.NewGuid();
        OperationsOutboxMessage earlier = OperationsOutboxMessage.Create("ops.test.order.v1", null, stream, "{}", now.AddSeconds(-10));
        OperationsOutboxMessage later = OperationsOutboxMessage.Create("ops.test.order.v1", null, stream, "{}", now.AddSeconds(-5));
        earlier.MarkFailed("ops.test.failed", now);
        await Save(earlier, later);

        await using OperationsDbContext context = _database.NewSystemContext();
        IReadOnlyList<Guid> leased = await new OperationsOutboxRepository(context).LeaseDueAsync(50, now, now.AddMinutes(2), CancellationToken.None);

        Assert.DoesNotContain(earlier.Id, leased);
        Assert.DoesNotContain(later.Id, leased);
    }

    [SqlServerFact]
    public async Task FeedPosition_StaleWriteIsRefused()
    {
        string feed = "test-" + Guid.NewGuid().ToString("N")[..8];
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await using (OperationsDbContext context = _database.NewSystemContext())
        {
            new OperationsFeedPositionRepository(context).Add(OperationsPlatformFeedPosition.Create(feed, now));
            await context.SaveChangesAsync();
        }

        await using OperationsDbContext one = _database.NewSystemContext();
        await using OperationsDbContext two = _database.NewSystemContext();
        OperationsPlatformFeedPosition first = (await new OperationsFeedPositionRepository(one).FindAsync(feed, CancellationToken.None))!;
        OperationsPlatformFeedPosition second = (await new OperationsFeedPositionRepository(two).FindAsync(feed, CancellationToken.None))!;
        first.MoveTo(5, now);
        second.MoveTo(7, now);

        Assert.True(await new OperationsUnitOfWork(one).SaveChangesAsync(CancellationToken.None));
        Assert.False(await new OperationsUnitOfWork(two).SaveChangesAsync(CancellationToken.None));
    }

    [SqlServerFact]
    public async Task Commit_CancelledBeforeItStarts_IsNotWritten()
    {
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();
        await using OperationsDbContext context = _database.NewSystemContext();
        context.Add(OperationsOutboxMessage.Create("ops.test.cancel.v1", null, Guid.NewGuid(), "{}", DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<OperationCanceledException>(() => new OperationsUnitOfWork(context).SaveChangesAsync(cancelled.Token));
    }

    private async Task Save(params OperationsOutboxMessage[] messages)
    {
        await using OperationsDbContext context = _database.NewSystemContext();
        OperationsOutboxRepository outbox = new(context);

        foreach (OperationsOutboxMessage message in messages)
        {
            outbox.Add(message);
        }

        await context.SaveChangesAsync();
    }
}
