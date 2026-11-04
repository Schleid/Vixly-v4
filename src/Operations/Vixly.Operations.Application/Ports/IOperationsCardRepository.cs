using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsCardRepository.cs
//
// PURPOSE:
// The company's Stripe account, card readers and card charges (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Read and change the account and the readers
// - Keep charges and find them by id, invoice or Stripe reference
//
// IMPORTANT NOTES:
// - Tenant-filtered like every Operations read
// =========================================================
public interface IOperationsCardRepository
{
    Task<OperationsCardProvider?> GetProviderAsync(CancellationToken cancellationToken);

    void Add(OperationsCardProvider provider);

    void Remove(OperationsCardProvider provider);

    Task<IReadOnlyList<OperationsCardReader>> ListReadersAsync(CancellationToken cancellationToken);

    Task<OperationsCardReader?> GetReaderAsync(Guid readerId, CancellationToken cancellationToken);

    void Add(OperationsCardReader reader);

    void Remove(OperationsCardReader reader);

    void Add(OperationsCardCharge charge);

    Task<OperationsCardCharge?> GetChargeAsync(Guid chargeId, CancellationToken cancellationToken);

    Task<OperationsCardCharge?> FindStartedAsync(Guid invoiceId, CancellationToken cancellationToken);

    // Review: the charges Stripe could still take money for, closed before the account is disconnected or replaced.
    Task<IReadOnlyList<OperationsCardCharge>> ListStartedAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsCardCharge>> ListForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken);
}
