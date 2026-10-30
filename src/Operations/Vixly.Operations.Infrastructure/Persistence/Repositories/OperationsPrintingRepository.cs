using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Printing;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsPrintingRepository.cs
//
// PURPOSE:
// Reads and writes label printers, dock print programs and print jobs (Slice 3B).
//
// RESPONSIBILITIES:
// - Implement IOperationsPrintingRepository over the Operations database
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1)
// =========================================================
internal sealed class OperationsPrintingRepository : IOperationsPrintingRepository
{
    private readonly OperationsDbContext _context;

    public OperationsPrintingRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsLabelPrinter printer) => _context.LabelPrinters.Add(printer ?? throw new ArgumentNullException(nameof(printer)));

    public void Add(OperationsPrintAgent agent) => _context.PrintAgents.Add(agent ?? throw new ArgumentNullException(nameof(agent)));

    public void Add(OperationsPrintJob job) => _context.PrintJobs.Add(job ?? throw new ArgumentNullException(nameof(job)));

    public Task<OperationsLabelPrinter?> GetPrinterAsync(Guid printerId, CancellationToken cancellationToken) =>
        _context.LabelPrinters.FirstOrDefaultAsync(item => item.Id == printerId, cancellationToken);

    public async Task<IReadOnlyList<OperationsLabelPrinter>> ListPrintersAsync(Guid warehouseId, CancellationToken cancellationToken) =>
        await _context.LabelPrinters.Where(item => item.WarehouseId == warehouseId).OrderBy(item => item.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsLabelPrinter>> ListPrintersByIdAsync(IReadOnlyCollection<Guid> printerIds, CancellationToken cancellationToken) =>
        printerIds is { Count: > 0 }
            ? await _context.LabelPrinters.AsNoTracking().Where(item => printerIds.Contains(item.Id)).ToListAsync(cancellationToken)
            : Array.Empty<OperationsLabelPrinter>();

    public Task<OperationsPrintAgent?> GetAgentAsync(Guid agentId, CancellationToken cancellationToken) =>
        _context.PrintAgents.FirstOrDefaultAsync(item => item.Id == agentId, cancellationToken);

    public async Task<IReadOnlyList<OperationsPrintAgent>> ListAgentsAsync(Guid warehouseId, CancellationToken cancellationToken) =>
        await _context.PrintAgents.AsNoTracking().Where(item => item.WarehouseId == warehouseId).OrderBy(item => item.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsPrintJob>> ListWaitingJobsAsync(Guid warehouseId, DateTimeOffset utcNow, int max, CancellationToken cancellationToken)
    {
        // A label that is no longer fresh is never printed: the clerk has moved on and would stick it on the wrong package.
        DateTimeOffset oldest = utcNow - OperationsPrintJob.FreshFor;
        return await _context.PrintJobs
            .Where(item => item.WarehouseId == warehouseId && item.Status == OperationsPrintJobStatus.Pending && item.CreatedUtc > oldest)
            .OrderBy(item => item.CreatedUtc)
            .Take(max)
            .ToListAsync(cancellationToken);
    }

    public Task<OperationsPrintJob?> GetJobAsync(Guid jobId, CancellationToken cancellationToken) =>
        _context.PrintJobs.FirstOrDefaultAsync(item => item.Id == jobId, cancellationToken);
}
