using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsLocationRepository.cs
//
// PURPOSE:
// Branches, warehouses and country contacts of the current tenant.
//
// RESPONSIBILITIES:
// - Implement IOperationsLocationRepository with EF Core
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1)
// - A code is checked across branches and warehouses
// =========================================================
internal sealed class OperationsLocationRepository : IOperationsLocationRepository
{
    private readonly OperationsDbContext _context;

    public OperationsLocationRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsBranch branch) => _context.Branches.Add(branch ?? throw new ArgumentNullException(nameof(branch)));

    public void Add(OperationsWarehouse warehouse) => _context.Warehouses.Add(warehouse ?? throw new ArgumentNullException(nameof(warehouse)));

    public void Add(OperationsCountryContact contact) => _context.CountryContacts.Add(contact ?? throw new ArgumentNullException(nameof(contact)));

    public void Remove(OperationsCountryContact contact) => _context.CountryContacts.Remove(contact ?? throw new ArgumentNullException(nameof(contact)));

    public Task<OperationsBranch?> GetBranchAsync(Guid branchId, CancellationToken cancellationToken) =>
        _context.Branches.FirstOrDefaultAsync(item => item.Id == branchId, cancellationToken);

    public Task<OperationsWarehouse?> GetWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken) =>
        _context.Warehouses.FirstOrDefaultAsync(item => item.Id == warehouseId, cancellationToken);

    public Task<OperationsCountryContact?> GetContactAsync(Guid contactId, CancellationToken cancellationToken) =>
        _context.CountryContacts.FirstOrDefaultAsync(item => item.Id == contactId, cancellationToken);

    public async Task<IReadOnlyList<OperationsBranch>> ListBranchesAsync(CancellationToken cancellationToken) =>
        await _context.Branches.OrderBy(item => item.Code).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsWarehouse>> ListWarehousesAsync(CancellationToken cancellationToken) =>
        await _context.Warehouses.OrderBy(item => item.Code).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsCountryContact>> ListContactsAsync(CancellationToken cancellationToken) =>
        await _context.CountryContacts.OrderBy(item => item.SortOrder).ThenBy(item => item.CountryCode).ToListAsync(cancellationToken);

    public async Task<bool> CodeTakenAsync(string code, Guid? exceptId, CancellationToken cancellationToken) =>
        await _context.Branches.AnyAsync(item => item.Code == code && item.Id != exceptId, cancellationToken)
        || await _context.Warehouses.AnyAsync(item => item.Code == code && item.Id != exceptId, cancellationToken);
}
