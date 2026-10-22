using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsShippingServiceRepository.cs
//
// PURPOSE:
// Reads and adds the tenant's shipping-service setups (Slice 1E).
//
// RESPONSIBILITIES:
// - List the setups of the current tenant
//
// IMPORTANT NOTES:
// - The unit of work saves
// =========================================================
internal sealed class OperationsShippingServiceRepository : IOperationsShippingServiceRepository
{
    private readonly OperationsDbContext _context;

    public OperationsShippingServiceRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsShippingServiceSetup setup) => _context.ShippingServiceSetups.Add(setup ?? throw new ArgumentNullException(nameof(setup)));

    public async Task<IReadOnlyList<OperationsShippingServiceSetup>> ListAsync(CancellationToken cancellationToken) =>
        await _context.ShippingServiceSetups.ToListAsync(cancellationToken);
}
