using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformDepartmentRepository.cs
//
// PURPOSE:
// EF implementation of IPlatformDepartmentRepository.
//
// RESPONSIBILITIES:
// - Load a department
// - Check whether a department name is taken
//
// IMPORTANT NOTES:
// - Name checks use the database collation (case-insensitive),
//   the same rule as the unique index on Name
// =========================================================
internal sealed class PlatformDepartmentRepository : IPlatformDepartmentRepository
{
    private readonly PlatformDbContext _context;

    public PlatformDepartmentRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformDepartment?> GetAsync(Guid platformDepartmentId, CancellationToken cancellationToken)
    {
        return _context.Departments.FirstOrDefaultAsync(department => department.Id == platformDepartmentId, cancellationToken);
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludedDepartmentId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(name);

        return _context.Departments.AnyAsync(
            department => department.Name == name && (excludedDepartmentId == null || department.Id != excludedDepartmentId),
            cancellationToken);
    }

    public void Add(PlatformDepartment department)
    {
        ArgumentNullException.ThrowIfNull(department);

        _context.Departments.Add(department);
    }
}
