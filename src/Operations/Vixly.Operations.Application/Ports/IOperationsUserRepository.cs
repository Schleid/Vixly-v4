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

    // A work email at any company (not only this one), once the invitation is accepted: such an email never becomes a customer email (review item 3).
    Task<bool> IsStaffEmailAnywhereAsync(EmailAddress email, CancellationToken cancellationToken);

    // Slice 7 step 4 (R-8): the partner agents of a warehouse, by name.
    Task<IReadOnlyList<OperationsUser>> ListPartnerAgentsAsync(Guid warehouseId, CancellationToken cancellationToken);

    // A page of staff, by name; the search matches the name or the exact email (Slice 1D).
    Task<(IReadOnlyList<OperationsUser> Items, int TotalCount)> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
}
