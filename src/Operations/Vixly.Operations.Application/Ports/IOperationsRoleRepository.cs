using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsRoleRepository.cs
//
// PURPOSE:
// Roles and role grants of the tenant (Slice 1D).
//
// RESPONSIBILITIES:
// - Add, read and list roles
// - Add and list grants
//
// IMPORTANT NOTES:
// - Tenant-scoped by the query filter
// =========================================================
public interface IOperationsRoleRepository
{
    void Add(OperationsRole role);

    void Add(OperationsRoleGrant grant);

    Task<OperationsRole?> GetAsync(Guid roleId, CancellationToken cancellationToken);

    Task<OperationsRole?> FindAdministratorAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsRole>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsRoleGrant>> ListGrantsAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsRoleGrant>> ListActiveGrantsAsync(CancellationToken cancellationToken);

    Task<OperationsRoleGrant?> GetGrantAsync(Guid grantId, CancellationToken cancellationToken);
}
