using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsUserRepository.cs
//
// PURPOSE:
// Staff users of the current tenant.
//
// RESPONSIBILITIES:
// - Implement IOperationsUserRepository with EF Core
//
// IMPORTANT NOTES:
// - Tenant-filtered by the query filter (D-F1)
// =========================================================
internal sealed class OperationsUserRepository : IOperationsUserRepository
{
    private readonly OperationsDbContext _context;

    public OperationsUserRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        _context.Users.Add(user);
    }

    public Task<OperationsUser?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _context.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public Task<OperationsUser?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);
        return _context.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken)
    {
        return _context.Users.AnyAsync(cancellationToken);
    }

    // Slice 7 step 4 (R-8): this company's partner agents of one warehouse (the tenant filter applies).
    public async Task<IReadOnlyList<OperationsUser>> ListPartnerAgentsAsync(Guid warehouseId, CancellationToken cancellationToken) =>
        await _context.Users.Where(user => user.PartnerWarehouseId == warehouseId).OrderBy(user => user.FullName).Take(200).ToListAsync(cancellationToken);

    // Across companies on purpose (one of two reviewed cross-tenant reads, design note item 3); only a yes or a no leaves this method.
    // Only a member who accepted the invitation and is not deactivated holds the email: an invitation never sent on blocks nobody.

    public Task<bool> IsStaffEmailAnywhereAsync(EmailAddress email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);
        return _context.Users.IgnoreQueryFilters().AnyAsync(user => user.Email == email && user.Account.PasswordHash != null
            && user.Account.Status != OperationsUserAccountStatus.Deactivated, cancellationToken);
    }

    public async Task<(IReadOnlyList<OperationsUser> Items, int TotalCount)> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<OperationsUser> query = _context.Users;
        string text = search?.Trim() ?? string.Empty;

        if (text.Length > 0)
        {
            query = EmailAddress.TryCreate(text, out EmailAddress? email)
                ? query.Where(user => user.Email == email)
                : query.Where(user => user.FullName.Contains(text));
        }

        int total = await query.CountAsync(cancellationToken);
        List<OperationsUser> items = await query
            .OrderByDescending(user => user.IsOwner)
            .ThenBy(user => user.FullName)
            .Skip((Math.Max(page, 1) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
