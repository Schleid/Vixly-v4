using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Screens;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsScreenRepository.cs
//
// PURPOSE:
// EF Core store of the company's customer screens and what customers accepted or signed on them (Slice 5F step 3).
//
// RESPONSIBILITIES:
// - Implement IOperationsScreenRepository
//
// IMPORTANT NOTES:
// - Tenant-filtered by the model (D-F1); a screen is found by the hash of its secret or of its pairing code, never by the secret itself
// =========================================================
internal sealed class OperationsScreenRepository : IOperationsScreenRepository
{
    private readonly OperationsDbContext _context;

    public OperationsScreenRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsCustomerScreen screen) => _context.CustomerScreens.Add(screen ?? throw new ArgumentNullException(nameof(screen)));

    public void Remove(OperationsCustomerScreen screen) => _context.CustomerScreens.Remove(screen ?? throw new ArgumentNullException(nameof(screen)));

    public async Task<IReadOnlyList<OperationsCustomerScreen>> ListAsync(CancellationToken cancellationToken) =>
        await _context.CustomerScreens.OrderBy(screen => screen.Name).Take(200).ToListAsync(cancellationToken);

    public Task<OperationsCustomerScreen?> GetAsync(Guid screenId, CancellationToken cancellationToken) =>
        _context.CustomerScreens.FirstOrDefaultAsync(screen => screen.Id == screenId, cancellationToken);

    public Task<OperationsCustomerScreen?> FindBySecretAsync(string secretHash, CancellationToken cancellationToken) =>
        string.IsNullOrEmpty(secretHash) ? Task.FromResult<OperationsCustomerScreen?>(null)
            : _context.CustomerScreens.FirstOrDefaultAsync(screen => screen.SecretHash == secretHash, cancellationToken);

    public Task<OperationsCustomerScreen?> FindByCodeAsync(string pairingCodeHash, CancellationToken cancellationToken) =>
        string.IsNullOrEmpty(pairingCodeHash) ? Task.FromResult<OperationsCustomerScreen?>(null)
            : _context.CustomerScreens.FirstOrDefaultAsync(screen => screen.PairingCodeHash == pairingCodeHash, cancellationToken);

    public void Add(OperationsScreenRecord record) => _context.ScreenRecords.Add(record ?? throw new ArgumentNullException(nameof(record)));

    public async Task<IReadOnlyList<OperationsScreenRecord>> ListRecordsAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        await _context.ScreenRecords.AsNoTracking().Where(record => record.InvoiceId == invoiceId).OrderByDescending(record => record.CreatedUtc).Take(50).ToListAsync(cancellationToken);

    public async Task<DateTimeOffset?> ZelleAcceptedAsync(Guid invoiceId, string termsHash, DateTimeOffset since, CancellationToken cancellationToken) =>
        await _context.ScreenRecords.Where(record => record.InvoiceId == invoiceId && record.Kind == OperationsScreenRecordKind.ZelleTerms && record.TermsHash == termsHash
                && record.CreatedUtc >= since)
            .OrderByDescending(record => record.CreatedUtc).Select(record => (DateTimeOffset?)record.CreatedUtc).FirstOrDefaultAsync(cancellationToken);
}
