using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsOutboxRepository.cs
//
// PURPOSE:
// Outbox storage with an atomic lease (as Platform).
//
// RESPONSIBILITIES:
// - Add, lease and load messages
//
// IMPORTANT NOTES:
// - UPDLOCK and READPAST: two hosts never lease the same message
// - A message waits while an older one of its stream is unprocessed
// =========================================================
internal sealed class OperationsOutboxRepository : IOperationsOutboxRepository
{
    private readonly OperationsDbContext _context;

    public OperationsOutboxRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsOutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _context.OutboxMessages.Add(message);
    }

    public async Task<IReadOnlyList<Guid>> LeaseDueAsync(int max, DateTimeOffset utcNow, DateTimeOffset leasedUntilUtc, CancellationToken cancellationToken)
    {
        return await _context.Database
            .SqlQuery<Guid>($"""
                WITH due AS (
                    SELECT TOP ({max}) message.Id, message.LeasedUntilUtc
                    FROM ops.OutboxMessages AS message WITH (UPDLOCK, READPAST, ROWLOCK)
                    WHERE message.ProcessedUtc IS NULL
                      AND message.NextAttemptUtc IS NOT NULL
                      AND message.NextAttemptUtc <= {utcNow}
                      AND (message.LeasedUntilUtc IS NULL OR message.LeasedUntilUtc <= {utcNow})
                      AND NOT EXISTS (
                          SELECT 1
                          FROM ops.OutboxMessages AS earlier
                          WHERE earlier.StreamId = message.StreamId
                            AND earlier.ProcessedUtc IS NULL
                            AND earlier.CreatedUtc < message.CreatedUtc)
                    ORDER BY message.CreatedUtc)
                UPDATE due
                SET LeasedUntilUtc = {leasedUntilUtc}
                OUTPUT inserted.Id AS Value;
                """)
            .ToListAsync(cancellationToken);
    }

    public Task<OperationsOutboxMessage?> GetAsync(Guid messageId, CancellationToken cancellationToken)
    {
        return _context.OutboxMessages.FirstOrDefaultAsync(item => item.Id == messageId, cancellationToken);
    }
}
