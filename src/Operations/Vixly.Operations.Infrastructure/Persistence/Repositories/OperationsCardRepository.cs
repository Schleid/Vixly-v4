using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsCardRepository.cs
//
// PURPOSE:
// EF Core store of the company's Stripe account, its card readers and card charges (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Implement IOperationsCardRepository
//
// IMPORTANT NOTES:
// - Tenant-filtered by the model (D-F1)
// =========================================================
internal sealed class OperationsCardRepository : IOperationsCardRepository
{
    private readonly OperationsDbContext _context;

    public OperationsCardRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<OperationsCardProvider?> GetProviderAsync(CancellationToken cancellationToken) => _context.CardProviders.FirstOrDefaultAsync(cancellationToken);

    public void Add(OperationsCardProvider provider) => _context.CardProviders.Add(provider ?? throw new ArgumentNullException(nameof(provider)));

    public void Remove(OperationsCardProvider provider) => _context.CardProviders.Remove(provider ?? throw new ArgumentNullException(nameof(provider)));

    public async Task<IReadOnlyList<OperationsCardReader>> ListReadersAsync(CancellationToken cancellationToken) =>
        await _context.CardReaders.OrderBy(reader => reader.Label).Take(200).ToListAsync(cancellationToken);

    public Task<OperationsCardReader?> GetReaderAsync(Guid readerId, CancellationToken cancellationToken) =>
        _context.CardReaders.FirstOrDefaultAsync(reader => reader.Id == readerId, cancellationToken);

    public void Add(OperationsCardReader reader) => _context.CardReaders.Add(reader ?? throw new ArgumentNullException(nameof(reader)));

    public void Remove(OperationsCardReader reader) => _context.CardReaders.Remove(reader ?? throw new ArgumentNullException(nameof(reader)));

    public void Add(OperationsCardCharge charge) => _context.CardCharges.Add(charge ?? throw new ArgumentNullException(nameof(charge)));

    public Task<OperationsCardCharge?> GetChargeAsync(Guid chargeId, CancellationToken cancellationToken) =>
        _context.CardCharges.FirstOrDefaultAsync(charge => charge.Id == chargeId, cancellationToken);

    public Task<OperationsCardCharge?> FindStartedAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        _context.CardCharges.FirstOrDefaultAsync(charge => charge.InvoiceId == invoiceId && charge.Status == OperationsCardChargeStatus.Started, cancellationToken);

    public async Task<IReadOnlyList<OperationsCardCharge>> ListStartedAsync(CancellationToken cancellationToken) =>
        await _context.CardCharges.Where(charge => charge.Status == OperationsCardChargeStatus.Started).OrderBy(charge => charge.CreatedUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsCardCharge>> ListForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        await _context.CardCharges.AsNoTracking().Where(charge => charge.InvoiceId == invoiceId).OrderByDescending(charge => charge.CreatedUtc).Take(50).ToListAsync(cancellationToken);
}
