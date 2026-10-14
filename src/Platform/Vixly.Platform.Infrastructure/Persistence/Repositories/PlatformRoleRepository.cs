using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformRoleRepository.cs
//
// PURPOSE:
// EF implementation of IPlatformRoleRepository.
//
// RESPONSIBILITIES:
// - Load a role with its permissions
// - List the system roles
// - Check whether a role name is taken
//
// IMPORTANT NOTES:
// - Name checks use the database collation (case-insensitive),
//   the same rule as the unique index on Name
// =========================================================
internal sealed class PlatformRoleRepository : IPlatformRoleRepository
{
    private readonly PlatformDbContext _context;

    public PlatformRoleRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformRole?> GetAsync(Guid platformRoleId, CancellationToken cancellationToken)
    {
        return _context.Roles
            .Include(role => role.Permissions)
            .FirstOrDefaultAsync(role => role.Id == platformRoleId, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformRole>> ListSystemAsync(CancellationToken cancellationToken)
    {
        return await _context.Roles
            .Include(role => role.Permissions)
            .Where(role => role.SystemKey != null)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludedRoleId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(name);

        return _context.Roles.AnyAsync(
            role => role.Name == name && (excludedRoleId == null || role.Id != excludedRoleId),
            cancellationToken);
    }

    public void Add(PlatformRole role)
    {
        ArgumentNullException.ThrowIfNull(role);

        _context.Roles.Add(role);
    }
}
