using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Jobs;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformJobRunRepository.cs
//
// PURPOSE:
// Stores the runs of Control's background jobs.
//
// RESPONSIBILITIES:
// - Add runs, prune old ones
//
// IMPORTANT NOTES:
// - Append only
// =========================================================
internal sealed class PlatformJobRunRepository : IPlatformJobRunRepository
{
    private readonly PlatformDbContext _context;

    public PlatformJobRunRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(PlatformJobRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        _context.JobRuns.Add(run);
    }

    public Task<int> DeleteCreatedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken)
    {
        return _context.JobRuns.Where(item => item.CreatedUtc < cutoffUtc).ExecuteDeleteAsync(cancellationToken);
    }
}
