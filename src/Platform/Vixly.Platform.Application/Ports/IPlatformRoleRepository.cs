using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformRoleRepository.cs
//
// PURPOSE:
// Loads and stores Platform roles for the use cases.
//
// RESPONSIBILITIES:
// - Load one role with its permissions
// - List the system roles for the deploy-time synchronisation
// - Answer whether a role name is taken
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure (EF Core)
// - Name comparison follows the database collation, which is
//   case-insensitive, matching the unique index
// =========================================================
public interface IPlatformRoleRepository
{
    Task<PlatformRole?> GetAsync(Guid platformRoleId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformRole>> ListSystemAsync(CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? excludedRoleId, CancellationToken cancellationToken);

    void Add(PlatformRole role);
}
