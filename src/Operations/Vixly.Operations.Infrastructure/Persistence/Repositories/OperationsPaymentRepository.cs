using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsPaymentRepository.cs
//
// PURPOSE:
// EF Core store of payments, cash drawers and refunds (Slice 5F step 2).
//
// RESPONSIBILITIES:
// - Implement IOperationsPaymentRepository
//
// IMPORTANT NOTES:
// - Tenant-filtered by the model (D-F1)
// =========================================================
internal sealed class OperationsPaymentRepository : IOperationsPaymentRepository
{
    private readonly OperationsDbContext _context;

    public OperationsPaymentRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsPayment payment) => _context.Payments.Add(payment ?? throw new ArgumentNullException(nameof(payment)));

    public void Add(OperationsCashDrawer drawer) => _context.CashDrawers.Add(drawer ?? throw new ArgumentNullException(nameof(drawer)));

    public void Add(OperationsRefund refund) => _context.Refunds.Add(refund ?? throw new ArgumentNullException(nameof(refund)));

    public void Remove(OperationsRefund refund) => _context.Refunds.Remove(refund ?? throw new ArgumentNullException(nameof(refund)));

    public Task<OperationsPayment?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken) =>
        _context.Payments.FirstOrDefaultAsync(payment => payment.Id == paymentId, cancellationToken);

    public Task<OperationsPayment?> FindPaymentByKeyAsync(Guid requestKey, CancellationToken cancellationToken) =>
        _context.Payments.AsNoTracking().FirstOrDefaultAsync(payment => payment.RequestKey == requestKey, cancellationToken);

    public Task<OperationsRefund?> FindRefundByKeyAsync(Guid requestKey, CancellationToken cancellationToken) =>
        _context.Refunds.FirstOrDefaultAsync(refund => refund.RequestKey == requestKey, cancellationToken); // tracked: a pending card refund is settled from it

    public async Task<IReadOnlyList<OperationsPayment>> ListInvoicePaymentsAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        await _context.Payments.AsNoTracking().Where(payment => payment.InvoiceId == invoiceId).OrderBy(payment => payment.TakenUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsRefund>> ListInvoiceRefundsAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        await _context.Refunds.AsNoTracking().Where(refund => refund.InvoiceId == invoiceId).OrderBy(refund => refund.CreatedUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsPayment>> ListPendingAsync(IReadOnlyCollection<Guid> warehouseIds, int take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);

        return await _context.Payments.AsNoTracking().Where(payment => payment.Pending && warehouseIds.Contains(payment.WarehouseId))
            .OrderBy(payment => payment.TakenUtc).Take(take).ToListAsync(cancellationToken);
    }

    public Task<OperationsCashDrawer?> GetDrawerAsync(Guid drawerId, CancellationToken cancellationToken) =>
        _context.CashDrawers.FirstOrDefaultAsync(drawer => drawer.Id == drawerId, cancellationToken);

    public Task<OperationsCashDrawer?> FindOpenDrawerAsync(Guid userId, Guid warehouseId, CancellationToken cancellationToken) =>
        _context.CashDrawers.FirstOrDefaultAsync(drawer => drawer.UserId == userId && drawer.WarehouseId == warehouseId && drawer.Status == OperationsCashDrawerStatus.Open, cancellationToken);

    public async Task<IReadOnlyList<OperationsCashDrawer>> ListOpenDrawersAsync(Guid userId, CancellationToken cancellationToken) =>
        await _context.CashDrawers.AsNoTracking().Where(drawer => drawer.UserId == userId && drawer.Status == OperationsCashDrawerStatus.Open).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsCashDrawer>> ListDrawersAsync(IReadOnlyCollection<Guid> warehouseIds, int take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);

        return await _context.CashDrawers.AsNoTracking().Where(drawer => warehouseIds.Contains(drawer.WarehouseId)).OrderByDescending(drawer => drawer.OpenedUtc).Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsPayment>> ListDrawerPaymentsAsync(Guid drawerId, CancellationToken cancellationToken) =>
        await _context.Payments.AsNoTracking().Where(payment => payment.DrawerId == drawerId).OrderBy(payment => payment.TakenUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsRefund>> ListDrawerRefundsAsync(Guid drawerId, CancellationToken cancellationToken) =>
        await _context.Refunds.AsNoTracking().Where(refund => refund.DrawerId == drawerId).OrderBy(refund => refund.CreatedUtc).ToListAsync(cancellationToken);
}
