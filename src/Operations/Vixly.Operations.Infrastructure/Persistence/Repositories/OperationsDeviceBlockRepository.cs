using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsDeviceBlockRepository.cs
//
// PURPOSE:
// Stores devices blocked after a sign-in from another country (owner, R-6).
//
// RESPONSIBILITIES:
// - Add a block; find a device's open block; list the company's open blocks
//
// IMPORTANT NOTES:
// - The tenant query filter applies; at most 200 open blocks are listed
// =========================================================
internal sealed class OperationsDeviceBlockRepository : IOperationsDeviceBlockRepository
{
    private readonly OperationsDbContext _context;

    public OperationsDeviceBlockRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsDeviceBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        _context.DeviceBlocks.Add(block);
    }

    public Task<OperationsDeviceBlock?> FindOpenAsync(Guid deviceId, CancellationToken cancellationToken) =>
        _context.DeviceBlocks.FirstOrDefaultAsync(block => block.DeviceId == deviceId && block.UnblockedUtc == null, cancellationToken);

    public Task<OperationsDeviceBlock?> GetAsync(Guid blockId, CancellationToken cancellationToken) =>
        _context.DeviceBlocks.FirstOrDefaultAsync(block => block.Id == blockId, cancellationToken);

    public async Task<IReadOnlyList<OperationsDeviceBlock>> ListOpenAsync(CancellationToken cancellationToken) =>
        await _context.DeviceBlocks.AsNoTracking().Where(block => block.UnblockedUtc == null).OrderByDescending(block => block.CreatedUtc).Take(200).ToListAsync(cancellationToken);
}
