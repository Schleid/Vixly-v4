using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Billing;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsCreditRepository.cs
//
// PURPOSE:
// EF Core store of credit accounts and the invoices on them (Slice 5F step 2b).
//
// RESPONSIBILITIES:
// - Implement IOperationsCreditRepository
//
// IMPORTANT NOTES:
// - Tenant-filtered by the model (D-F1); the tenants list runs in the system scope
// =========================================================
internal sealed class OperationsCreditRepository : IOperationsCreditRepository
{
    private readonly OperationsDbContext _context;

    public OperationsCreditRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsCreditAccount account) => _context.CreditAccounts.Add(account ?? throw new ArgumentNullException(nameof(account)));

    public Task<OperationsCreditAccount?> FindByCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        _context.CreditAccounts.FirstOrDefaultAsync(account => account.CustomerId == customerId, cancellationToken);

    public async Task<IReadOnlyList<OperationsCreditAccount>> ListAsync(int take, CancellationToken cancellationToken) =>
        await _context.CreditAccounts.AsNoTracking().OrderByDescending(account => account.CreatedUtc).Take(take).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsInvoice>> ListAccountInvoicesAsync(IReadOnlyCollection<Guid> customerIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customerIds);

        return customerIds.Count == 0
            ? Array.Empty<OperationsInvoice>()
            : await Open(_context.Invoices.AsNoTracking().Where(invoice => customerIds.Contains(invoice.CustomerId) && invoice.OnAccount != OperationsAccountCharge.None))
                .OrderBy(invoice => invoice.IssuedUtc).ToListAsync(cancellationToken);
    }

    // Still owed (Owed, worked out in SQL): a paid invoice stays charged but is no longer on the account (review).
    private static IQueryable<OperationsInvoice> Open(IQueryable<OperationsInvoice> invoices) =>
        invoices.Where(invoice => invoice.Status == OperationsInvoiceStatus.Issued
            && invoice.Total - invoice.CreditedTotal - invoice.PaidTotal - invoice.PendingTotal + invoice.RefundedTotal > 0m);

    public async Task<IReadOnlyList<OperationsCreditAccount>> ListStatementsDueAsync(string month, int take, CancellationToken cancellationToken) =>
        await _context.CreditAccounts.Where(account => account.Status != OperationsCreditAccountStatus.Closed && (account.StatementMonth == null || string.Compare(account.StatementMonth, month) < 0))
            .OrderBy(account => account.CreatedUtc).Take(take).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<(Guid CustomerId, string? Code, string Name)>> ListMarkedWithoutAccountAsync(int take, CancellationToken cancellationToken) =>
        (await _context.Customers.AsNoTracking().Where(customer => customer.CreditAllowed && !_context.CreditAccounts.Any(account => account.CustomerId == customer.Id))
            .OrderBy(customer => customer.Code).Take(take).Select(customer => new { customer.Id, customer.Code, customer.FirstName, customer.LastName }).ToListAsync(cancellationToken))
        .Select(row => (row.Id, row.Code, row.FirstName + " " + row.LastName)).ToList();

    public async Task<IReadOnlyList<OperationsInvoice>> ListAwaitingAsync(int take, CancellationToken cancellationToken) =>
        await Open(_context.Invoices.AsNoTracking().Where(invoice => invoice.OnAccount == OperationsAccountCharge.AwaitingApproval))
            .OrderBy(invoice => invoice.AccountRequestedUtc).Take(take).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListTenantsWithAccountsAsync(CancellationToken cancellationToken) =>
        await _context.CreditAccounts.AsNoTracking().Where(account => account.Status != OperationsCreditAccountStatus.Closed).Select(account => account.TenantId).Distinct().Take(10_000)
            .ToListAsync(cancellationToken);
}
