using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformOutboxRepository.cs
//
// PURPOSE:
// EF implementation of IPlatformOutboxRepository (D-T17).
//
// RESPONSIBILITIES:
// - Add messages to the current unit of work
// - Lease due messages in one atomic statement
//
// IMPORTANT NOTES:
// - UPDLOCK and READPAST: two dispatchers never lease the same
//   row and never wait on each other
// - Only the oldest unprocessed message of a stream is due, so
//   messages of one tenant are handled in order
// - The lease statement commits by itself
// =========================================================
internal sealed class PlatformOutboxRepository : IPlatformOutboxRepository
{
    private readonly PlatformDbContext _context;

    public PlatformOutboxRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(PlatformOutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _context.OutboxMessages.Add(message);
    }

    public async Task<IReadOnlyList<Guid>> LeaseDueAsync(
        int maxCount,
        DateTimeOffset utcNow,
        DateTimeOffset leasedUntilUtc,
        CancellationToken cancellationToken)
    {
        return await _context.Database
            .SqlQuery<Guid>($"""
                WITH due AS (
                    SELECT TOP ({maxCount}) message.Id, message.LeasedUntilUtc
                    FROM platform.OutboxMessages AS message WITH (UPDLOCK, READPAST, ROWLOCK)
                    WHERE message.ProcessedUtc IS NULL
                      AND message.NextAttemptUtc IS NOT NULL
                      AND message.NextAttemptUtc <= {utcNow}
                      AND (message.LeasedUntilUtc IS NULL OR message.LeasedUntilUtc <= {utcNow})
                      AND NOT EXISTS (
                          SELECT 1
                          FROM platform.OutboxMessages AS earlier
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

    public Task<PlatformOutboxMessage?> GetAsync(Guid messageId, CancellationToken cancellationToken)
    {
        return _context.OutboxMessages.FirstOrDefaultAsync(item => item.Id == messageId, cancellationToken);
    }
}
