using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Jobs;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsJobRunRepository.cs
//
// PURPOSE:
// Job run storage.
//
// RESPONSIBILITIES:
// - Add runs
//
// IMPORTANT NOTES:
// - Append only
// =========================================================
internal sealed class OperationsJobRunRepository : IOperationsJobRunRepository
{
    private readonly OperationsDbContext _context;

    public OperationsJobRunRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsJobRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        _context.JobRuns.Add(run);
    }
}
