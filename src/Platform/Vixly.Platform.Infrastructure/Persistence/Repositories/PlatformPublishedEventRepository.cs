using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformPublishedEventRepository.cs
//
// PURPOSE:
// Stores and reads the public event feed (Slice 9, D-K3).
//
// RESPONSIBILITIES:
// - Add, find, page and prune feed rows
//
// IMPORTANT NOTES:
// - Pages read without tracking, by sequence
// =========================================================
internal sealed class PlatformPublishedEventRepository : IPlatformPublishedEventRepository
{
    private readonly PlatformDbContext _context;

    public PlatformPublishedEventRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(PlatformPublishedEvent item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _context.PublishedEvents.Add(item);
    }

    public Task<bool> ExistsAsync(Guid outboxMessageId, string type, CancellationToken cancellationToken)
    {
        return _context.PublishedEvents.AnyAsync(item => item.OutboxMessageId == outboxMessageId && item.Type == type, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformPublishedEvent>> ListAfterAsync(long after, int take, DateTimeOffset createdBeforeUtc, IReadOnlyCollection<string> types, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(types);

        return await _context.PublishedEvents.AsNoTracking()
            .Where(item => item.Sequence > after && item.CreatedUtc < createdBeforeUtc && types.Contains(item.Type))
            .OrderBy(item => item.Sequence)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> DeleteCreatedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken)
    {
        return _context.PublishedEvents.Where(item => item.CreatedUtc < cutoffUtc).ExecuteDeleteAsync(cancellationToken);
    }
}
