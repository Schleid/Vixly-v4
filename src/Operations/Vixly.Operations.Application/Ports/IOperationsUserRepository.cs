using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsUserRepository.cs
//
// PURPOSE:
// Staff users of the current tenant.
//
// RESPONSIBILITIES:
// - Add, load by id or email, and tell whether the tenant has staff yet
//
// IMPORTANT NOTES:
// - Tenant-filtered by the data layer (D-F1)
// =========================================================
public interface IOperationsUserRepository
{
    void Add(OperationsUser user);

    Task<OperationsUser?> GetAsync(Guid userId, CancellationToken cancellationToken);

    Task<OperationsUser?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    Task<bool> AnyAsync(CancellationToken cancellationToken);

    // A page of staff, by name; the search matches the name or the exact email (Slice 1D).
    Task<(IReadOnlyList<OperationsUser> Items, int TotalCount)> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
}
