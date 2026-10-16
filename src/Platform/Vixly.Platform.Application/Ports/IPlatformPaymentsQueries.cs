using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformPaymentsQueries.cs
//
// PURPOSE:
// Read-only queries for card payments: the transactions list and the clock's work lists.
//
// RESPONSIBILITIES:
// - Page card transactions with filters
// - Find invoices to charge automatically, stale transactions and refunds not yet sent
//
// IMPORTANT NOTES:
// - No-tracking projections (P-1); every list is bounded
// =========================================================
public interface IPlatformPaymentsQueries
{
    Task<PagedResult<PlatformCardTransactionItem>> ListTransactionsAsync(PlatformCardTransactionFilter filter, PagedRequest page, CancellationToken cancellationToken);

    // Issued invoices with a balance, due by now, whose tenant consented and that never had an automatic charge.
    Task<IReadOnlyList<Guid>> ListAutoChargeInvoicesAsync(DateTimeOffset utcNow, int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ListStaleTransactionsAsync(DateTimeOffset createdBeforeUtc, int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ListUnsentRefundsAsync(int take, CancellationToken cancellationToken);
}
