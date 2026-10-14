using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformDepartmentRepository.cs
//
// PURPOSE:
// Loads and stores Platform departments for the use cases.
//
// RESPONSIBILITIES:
// - Load one department
// - Answer whether a department name is taken
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure (EF Core)
// - Name comparison follows the database collation, which is
//   case-insensitive, matching the unique index
// =========================================================
public interface IPlatformDepartmentRepository
{
    Task<PlatformDepartment?> GetAsync(Guid platformDepartmentId, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? excludedDepartmentId, CancellationToken cancellationToken);

    void Add(PlatformDepartment department);
}
