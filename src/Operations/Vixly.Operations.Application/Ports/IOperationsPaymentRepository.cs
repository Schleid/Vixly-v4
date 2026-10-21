using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsPaymentRepository.cs
//
// PURPOSE:
// Reads and writes payments, cash drawers and refunds (Slice 5F step 2).
//
// RESPONSIBILITIES:
// - Add them; find them by id or form key; list an invoice's, a drawer's and those waiting for finance
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1); lists are bounded by the take given
// =========================================================
public interface IOperationsPaymentRepository
{
    void Add(OperationsPayment payment);

    void Add(OperationsCashDrawer drawer);

    void Add(OperationsRefund refund);

    // Tracked.
    Task<OperationsPayment?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken);

    Task<OperationsPayment?> FindPaymentByKeyAsync(Guid requestKey, CancellationToken cancellationToken);

    Task<OperationsRefund?> FindRefundByKeyAsync(Guid requestKey, CancellationToken cancellationToken);

    // Read only, oldest first.
    Task<IReadOnlyList<OperationsPayment>> ListInvoicePaymentsAsync(Guid invoiceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsRefund>> ListInvoiceRefundsAsync(Guid invoiceId, CancellationToken cancellationToken);

    // Read only, oldest first: payments with a tender waiting for finance in these warehouses.
    Task<IReadOnlyList<OperationsPayment>> ListPendingAsync(IReadOnlyCollection<Guid> warehouseIds, int take, CancellationToken cancellationToken);

    // Tracked.
    Task<OperationsCashDrawer?> GetDrawerAsync(Guid drawerId, CancellationToken cancellationToken);

    // Tracked: the person's open drawer at a warehouse.
    Task<OperationsCashDrawer?> FindOpenDrawerAsync(Guid userId, Guid warehouseId, CancellationToken cancellationToken);

    // Read only: the person's open drawers.
    Task<IReadOnlyList<OperationsCashDrawer>> ListOpenDrawersAsync(Guid userId, CancellationToken cancellationToken);

    // Read only, newest first.
    Task<IReadOnlyList<OperationsCashDrawer>> ListDrawersAsync(IReadOnlyCollection<Guid> warehouseIds, int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsPayment>> ListDrawerPaymentsAsync(Guid drawerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsRefund>> ListDrawerRefundsAsync(Guid drawerId, CancellationToken cancellationToken);
}
