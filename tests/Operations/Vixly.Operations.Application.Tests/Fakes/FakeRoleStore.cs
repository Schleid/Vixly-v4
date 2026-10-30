using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakeRoleStore.cs
//
// PURPOSE:
// An in-memory role and grant store for use case tests (Slice 1D).
//
// RESPONSIBILITIES:
// - Hold roles and grants; list active grants only where asked
//
// IMPORTANT NOTES:
// - Tests only
// =========================================================
internal sealed class FakeRoleStore : IOperationsRoleRepository
{
    public List<OperationsRole> Roles { get; } = new();

    public List<OperationsRoleGrant> Grants { get; } = new();

    public void Add(OperationsRole role) => Roles.Add(role);

    public void Add(OperationsRoleGrant grant) => Grants.Add(grant);

    public Task<OperationsRole?> GetAsync(Guid roleId, CancellationToken cancellationToken) => Task.FromResult(Roles.FirstOrDefault(role => role.Id == roleId));

    public Task<OperationsRole?> FindAdministratorAsync(CancellationToken cancellationToken) => Task.FromResult(Roles.FirstOrDefault(role => role.IsAdministrator));

    public Task<IReadOnlyList<OperationsRole>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OperationsRole>>(Roles.ToList());

    public Task<IReadOnlyList<OperationsRoleGrant>> ListGrantsAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsRoleGrant>>(Grants.Where(grant => grant.UserId == userId).ToList());

    public Task<IReadOnlyList<OperationsRoleGrant>> ListActiveGrantsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsRoleGrant>>(Grants.Where(grant => grant.IsActive).ToList());

    public Task<OperationsRoleGrant?> GetGrantAsync(Guid grantId, CancellationToken cancellationToken) => Task.FromResult(Grants.FirstOrDefault(grant => grant.Id == grantId));
}
