using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformPaymentsRepository.cs
//
// PURPOSE:
// Persistence of the merchant account, card profiles, saved cards, card transactions, payment links and provider events.
//
// RESPONSIBILITIES:
// - Load and add the Slice 5 records
//
// IMPORTANT NOTES:
// - Saved by IPlatformUnitOfWork together with invoices, history, audit and outbox
// =========================================================
public interface IPlatformPaymentsRepository
{
    Task<PlatformMerchantAccount?> GetMerchantAsync(CancellationToken cancellationToken);

    void AddMerchant(PlatformMerchantAccount account);

    Task<PlatformTenantCardProfile?> GetProfileAsync(Guid platformTenantId, CancellationToken cancellationToken);

    void AddProfile(PlatformTenantCardProfile profile);

    Task<IReadOnlyList<PlatformSavedCard>> ListCardsAsync(Guid platformTenantId, CancellationToken cancellationToken);

    Task<PlatformSavedCard?> GetCardAsync(Guid savedCardId, CancellationToken cancellationToken);

    Task<PlatformSavedCard?> GetCardByProviderIdAsync(string paymentMethodId, CancellationToken cancellationToken);

    void AddCard(PlatformSavedCard card);

    Task<PlatformCardTransaction?> GetTransactionAsync(Guid transactionId, CancellationToken cancellationToken);

    Task<PlatformCardTransaction?> FindTransactionAsync(string? checkoutId, string? paymentId, string? refundId, CancellationToken cancellationToken);

    Task<PlatformCardTransaction?> GetOpenChargeAsync(Guid platformInvoiceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformCardTransaction>> ListForInvoiceAsync(Guid platformInvoiceId, CancellationToken cancellationToken);

    void AddTransaction(PlatformCardTransaction transaction);

    Task<PlatformPaymentLink?> GetLinkAsync(string tokenHash, CancellationToken cancellationToken);

    Task<PlatformPaymentLink?> GetLinkByIdAsync(Guid linkId, CancellationToken cancellationToken);

    void AddLink(PlatformPaymentLink link);

    Task<PlatformProviderEvent?> GetEventAsync(Guid recordId, CancellationToken cancellationToken);

    Task<bool> EventSeenAsync(PlatformCardProvider provider, string providerEventId, CancellationToken cancellationToken);

    void AddEvent(PlatformProviderEvent providerEvent);
}
