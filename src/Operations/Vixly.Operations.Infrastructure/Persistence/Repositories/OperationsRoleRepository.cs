using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsRoleRepository.cs
//
// PURPOSE:
// Roles and role grants in SQL Server (Slice 1D).
//
// RESPONSIBILITIES:
// - Add, read and list roles and grants
//
// IMPORTANT NOTES:
// - Tenant-scoped by the query filter
// =========================================================
internal sealed class OperationsRoleRepository : IOperationsRoleRepository
{
    private readonly OperationsDbContext _context;

    public OperationsRoleRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsRole role) => _context.Roles.Add(role ?? throw new ArgumentNullException(nameof(role)));

    public void Add(OperationsRoleGrant grant) => _context.RoleGrants.Add(grant ?? throw new ArgumentNullException(nameof(grant)));

    public Task<OperationsRole?> GetAsync(Guid roleId, CancellationToken cancellationToken) =>
        _context.Roles.FirstOrDefaultAsync(role => role.Id == roleId, cancellationToken);

    public Task<OperationsRole?> FindAdministratorAsync(CancellationToken cancellationToken) =>
        _context.Roles.FirstOrDefaultAsync(role => role.IsAdministrator, cancellationToken);

    public async Task<IReadOnlyList<OperationsRole>> ListAsync(CancellationToken cancellationToken) =>
        await _context.Roles.OrderByDescending(role => role.IsAdministrator).ThenBy(role => role.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsRoleGrant>> ListGrantsAsync(Guid userId, CancellationToken cancellationToken) =>
        await _context.RoleGrants.Where(grant => grant.UserId == userId).OrderByDescending(grant => grant.GrantedUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsRoleGrant>> ListActiveGrantsAsync(CancellationToken cancellationToken) =>
        await _context.RoleGrants.Where(grant => grant.RevokedUtc == null).ToListAsync(cancellationToken);

    public Task<OperationsRoleGrant?> GetGrantAsync(Guid grantId, CancellationToken cancellationToken) =>
        _context.RoleGrants.FirstOrDefaultAsync(grant => grant.Id == grantId, cancellationToken);
}
