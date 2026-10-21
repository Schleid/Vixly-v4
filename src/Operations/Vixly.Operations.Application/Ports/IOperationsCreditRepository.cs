using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsCreditRepository.cs
//
// PURPOSE:
// Reads and writes customers' credit accounts and the invoices on them (Slice 5F step 2b).
//
// RESPONSIBILITIES:
// - Add accounts; find one by id or customer
// - List accounts, an account's invoices, the charges waiting for the credit department
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1), except the tenants list for the monthly notice (system scope)
// =========================================================
public interface IOperationsCreditRepository
{
    void Add(OperationsCreditAccount account);

    // Tracked.
    Task<OperationsCreditAccount?> FindByCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    // Read only, newest first.
    Task<IReadOnlyList<OperationsCreditAccount>> ListAsync(int take, CancellationToken cancellationToken);

    // Tracked, oldest first: the accounts not closed whose last statement notice is from an earlier month ("yyyy-MM" compares in order).
    Task<IReadOnlyList<OperationsCreditAccount>> ListStatementsDueAsync(string month, int take, CancellationToken cancellationToken);

    // Read only: customers marked "credit allowed" before credit accounts existed, still without one (for the credit department to open).
    Task<IReadOnlyList<(Guid CustomerId, string? Code, string Name)>> ListMarkedWithoutAccountAsync(int take, CancellationToken cancellationToken);

    // Read only: the issued invoices still owed that are billed (or waiting to be billed) to these customers' accounts.
    Task<IReadOnlyList<Domain.Billing.OperationsInvoice>> ListAccountInvoicesAsync(IReadOnlyCollection<Guid> customerIds, CancellationToken cancellationToken);

    // Read only, oldest first: charges above a limit waiting for the credit department, still owed.
    Task<IReadOnlyList<Domain.Billing.OperationsInvoice>> ListAwaitingAsync(int take, CancellationToken cancellationToken);

    // System scope: the companies with an active account (the monthly notice).
    Task<IReadOnlyList<Guid>> ListTenantsWithAccountsAsync(CancellationToken cancellationToken);
}
