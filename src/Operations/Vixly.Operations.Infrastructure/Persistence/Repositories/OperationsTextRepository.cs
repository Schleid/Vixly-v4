using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Texts;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsTextRepository.cs
//
// PURPOSE:
// EF Core store of the company's Twilio account and its text message log (Slice 9).
//
// RESPONSIBILITIES:
// - Implement IOperationsTextRepository
//
// IMPORTANT NOTES:
// - Tenant-filtered by the model (D-F1)
// =========================================================
internal sealed class OperationsTextRepository : IOperationsTextRepository
{
    private readonly OperationsDbContext _context;

    public OperationsTextRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<OperationsTextProvider?> GetProviderAsync(CancellationToken cancellationToken) => _context.TextProviders.FirstOrDefaultAsync(cancellationToken);

    public void Add(OperationsTextProvider provider) => _context.TextProviders.Add(provider ?? throw new ArgumentNullException(nameof(provider)));

    public void Remove(OperationsTextProvider provider) => _context.TextProviders.Remove(provider ?? throw new ArgumentNullException(nameof(provider)));

    public void Add(OperationsTextMessage message) => _context.TextMessages.Add(message ?? throw new ArgumentNullException(nameof(message)));

    public Task<OperationsTextMessage?> FindMessageAsync(string providerId, CancellationToken cancellationToken) =>
        _context.TextMessages.FirstOrDefaultAsync(message => message.ProviderId == providerId, cancellationToken);

    public async Task<IReadOnlyList<OperationsTextMessage>> ListMessagesAsync(int take, CancellationToken cancellationToken) =>
        await _context.TextMessages.AsNoTracking().OrderByDescending(message => message.CreatedUtc).Take(Math.Clamp(take, 1, 500)).ToListAsync(cancellationToken);
}
