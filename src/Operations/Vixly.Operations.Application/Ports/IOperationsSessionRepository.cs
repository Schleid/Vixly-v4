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

    // Member activity (owner, 2026-10-21): the company's sign-ins, newest first, of one member or all.
    Task<(IReadOnlyList<OperationsUserSession> Items, int TotalCount)> ListActivityAsync(Guid? userId, int page, int pageSize, CancellationToken cancellationToken);
}
