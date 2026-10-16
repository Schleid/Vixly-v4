using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformPaymentsRepository.cs
//
// PURPOSE:
// EF Core persistence of the Slice 5 records.
//
// RESPONSIBILITIES:
// - Load and add merchant account, profiles, cards, transactions, links and events
//
// IMPORTANT NOTES:
// - Tracked reads; the unit of work saves
// =========================================================
internal sealed class PlatformPaymentsRepository : IPlatformPaymentsRepository
{
    private readonly PlatformDbContext _context;

    public PlatformPaymentsRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PlatformMerchantAccount?> GetMerchantAsync(CancellationToken cancellationToken)
    {
        return _context.MerchantAccounts.Local.FirstOrDefault(item => item.Key == PlatformPaymentsLimits.SingletonKey)
            ?? await _context.MerchantAccounts.FirstOrDefaultAsync(item => item.Key == PlatformPaymentsLimits.SingletonKey, cancellationToken);
    }

    public void AddMerchant(PlatformMerchantAccount account) => _context.MerchantAccounts.Add(account);

    public async Task<PlatformTenantCardProfile?> GetProfileAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return _context.TenantCardProfiles.Local.FirstOrDefault(item => item.PlatformTenantId == platformTenantId)
            ?? await _context.TenantCardProfiles.FirstOrDefaultAsync(item => item.PlatformTenantId == platformTenantId, cancellationToken);
    }

    public void AddProfile(PlatformTenantCardProfile profile) => _context.TenantCardProfiles.Add(profile);

    public async Task<IReadOnlyList<PlatformSavedCard>> ListCardsAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        List<PlatformSavedCard> stored = await _context.SavedCards.Where(item => item.PlatformTenantId == platformTenantId).ToListAsync(cancellationToken);
        return stored.Union(_context.SavedCards.Local.Where(item => item.PlatformTenantId == platformTenantId)).ToArray();
    }

    public Task<PlatformSavedCard?> GetCardAsync(Guid savedCardId, CancellationToken cancellationToken)
    {
        return _context.SavedCards.FirstOrDefaultAsync(item => item.Id == savedCardId, cancellationToken);
    }

    public async Task<PlatformSavedCard?> GetCardByProviderIdAsync(string paymentMethodId, CancellationToken cancellationToken)
    {
        return _context.SavedCards.Local.FirstOrDefault(item => item.ProviderPaymentMethodId == paymentMethodId)
            ?? await _context.SavedCards.FirstOrDefaultAsync(item => item.ProviderPaymentMethodId == paymentMethodId, cancellationToken);
    }

    public void AddCard(PlatformSavedCard card) => _context.SavedCards.Add(card);

    public Task<PlatformCardTransaction?> GetTransactionAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        return _context.CardTransactions.FirstOrDefaultAsync(item => item.Id == transactionId, cancellationToken);
    }

    public Task<PlatformCardTransaction?> FindTransactionAsync(string? checkoutId, string? paymentId, string? refundId, CancellationToken cancellationToken)
    {
        return _context.CardTransactions.FirstOrDefaultAsync(
            item => (checkoutId != null && item.ProviderCheckoutId == checkoutId)
                || (refundId != null && item.ProviderRefundId == refundId)
                || (paymentId != null && item.Kind == PlatformCardTransactionKind.Charge && item.ProviderPaymentId == paymentId),
            cancellationToken);
    }

    public Task<PlatformCardTransaction?> GetOpenChargeAsync(Guid platformInvoiceId, CancellationToken cancellationToken)
    {
        return _context.CardTransactions.FirstOrDefaultAsync(
            item => item.PlatformInvoiceId == platformInvoiceId && item.Kind == PlatformCardTransactionKind.Charge
                && (item.Status == PlatformCardTransactionStatus.Created || item.Status == PlatformCardTransactionStatus.Pending || item.Status == PlatformCardTransactionStatus.RequiresAction),
            cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformCardTransaction>> ListForInvoiceAsync(Guid platformInvoiceId, CancellationToken cancellationToken)
    {
        return await _context.CardTransactions.Where(item => item.PlatformInvoiceId == platformInvoiceId).OrderBy(item => item.CreatedUtc).ToListAsync(cancellationToken);
    }

    public void AddTransaction(PlatformCardTransaction transaction) => _context.CardTransactions.Add(transaction);

    public Task<PlatformPaymentLink?> GetLinkAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _context.PaymentLinks.FirstOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);
    }

    public Task<PlatformPaymentLink?> GetLinkByIdAsync(Guid linkId, CancellationToken cancellationToken)
    {
        return _context.PaymentLinks.FirstOrDefaultAsync(item => item.Id == linkId, cancellationToken);
    }

    public void AddLink(PlatformPaymentLink link) => _context.PaymentLinks.Add(link);

    public Task<PlatformProviderEvent?> GetEventAsync(Guid recordId, CancellationToken cancellationToken)
    {
        return _context.ProviderEvents.FirstOrDefaultAsync(item => item.Id == recordId, cancellationToken);
    }

    public Task<bool> EventSeenAsync(PlatformCardProvider provider, string providerEventId, CancellationToken cancellationToken)
    {
        return _context.ProviderEvents.AsNoTracking().AnyAsync(item => item.Provider == provider && item.ProviderEventId == providerEventId, cancellationToken);
    }

    public void AddEvent(PlatformProviderEvent providerEvent) => _context.ProviderEvents.Add(providerEvent);
}
