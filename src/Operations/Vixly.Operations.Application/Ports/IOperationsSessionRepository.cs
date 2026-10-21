using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsSessionRepository.cs
//
// PURPOSE:
// Staff sessions of the current tenant.
//
// RESPONSIBILITIES:
// - Add, find by token hash, load, list the open sessions of a user
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1)
// =========================================================
public interface IOperationsSessionRepository
{
    void Add(OperationsUserSession session);

    Task<OperationsUserSession?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<OperationsUserSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsUserSession>> ListOpenAsync(Guid userId, CancellationToken cancellationToken);
}
