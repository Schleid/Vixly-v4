using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Platform;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsFeedPositionRepository.cs
//
// PURPOSE:
// Feed position storage.
//
// RESPONSIBILITIES:
// - Find or add a feed position
//
// IMPORTANT NOTES:
// - One row per feed
// =========================================================
internal sealed class OperationsFeedPositionRepository : IOperationsFeedPositionRepository
{
    private readonly OperationsDbContext _context;

    public OperationsFeedPositionRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<OperationsPlatformFeedPosition?> FindAsync(string feed, CancellationToken cancellationToken)
    {
        return _context.PlatformFeedPositions.FirstOrDefaultAsync(item => item.Feed == feed, cancellationToken);
    }

    public void Add(OperationsPlatformFeedPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        _context.PlatformFeedPositions.Add(position);
    }
}
