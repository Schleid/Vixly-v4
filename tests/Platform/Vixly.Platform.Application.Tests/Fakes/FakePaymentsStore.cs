using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakePaymentsStore.cs
//
// PURPOSE:
// In-memory card-payment ports for the Slice 5 use-case tests.
//
// RESPONSIBILITIES:
// - Hold the merchant account, profiles, cards, transactions, links and events
// - Answer the clock's work lists; record emails
//
// IMPORTANT NOTES:
// - Test double only; the open-charge rule is checked like the database's unique index
// =========================================================
internal sealed class FakePaymentsStore : IPlatformPaymentsRepository, IPlatformPaymentsQueries, IPlatformPaymentsMailer
{
    private readonly FakeBillingStore _billing;

    public FakePaymentsStore(FakeBillingStore billing)
    {
        _billing = billing;
    }

    public PlatformMerchantAccount? Merchant { get; set; }

    public List<PlatformTenantCardProfile> Profiles { get; } = new();

    public List<PlatformSavedCard> Cards { get; } = new();

    public List<PlatformCardTransaction> Transactions { get; } = new();

    public List<PlatformPaymentLink> Links { get; } = new();

    public List<PlatformProviderEvent> Events { get; } = new();

    public List<string> Mailed { get; } = new();

    public Task<PlatformMerchantAccount?> GetMerchantAsync(CancellationToken cancellationToken) => Task.FromResult(Merchant);

    public void AddMerchant(PlatformMerchantAccount account) => Merchant = account;

    public Task<PlatformTenantCardProfile?> GetProfileAsync(Guid platformTenantId, CancellationToken cancellationToken) => Task.FromResult(Profiles.FirstOrDefault(item => item.PlatformTenantId == platformTenantId));

    public void AddProfile(PlatformTenantCardProfile profile) => Profiles.Add(profile);

    public Task<IReadOnlyList<PlatformSavedCard>> ListCardsAsync(Guid platformTenantId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PlatformSavedCard>>(Cards.Where(item => item.PlatformTenantId == platformTenantId).ToArray());

    public Task<PlatformSavedCard?> GetCardAsync(Guid savedCardId, CancellationToken cancellationToken) => Task.FromResult(Cards.FirstOrDefault(item => item.Id == savedCardId));

    public Task<PlatformSavedCard?> GetCardByProviderIdAsync(string paymentMethodId, CancellationToken cancellationToken) => Task.FromResult(Cards.FirstOrDefault(item => item.ProviderPaymentMethodId == paymentMethodId));

    public void AddCard(PlatformSavedCard card) => Cards.Add(card);

    public Task<PlatformCardTransaction?> GetTransactionAsync(Guid transactionId, CancellationToken cancellationToken) => Task.FromResult(Transactions.FirstOrDefault(item => item.Id == transactionId));

    public Task<PlatformCardTransaction?> FindTransactionAsync(string? checkoutId, string? paymentId, string? refundId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Transactions.FirstOrDefault(item =>
            (checkoutId is not null && item.ProviderCheckoutId == checkoutId)
            || (refundId is not null && item.ProviderRefundId == refundId)
            || (paymentId is not null && item.Kind == PlatformCardTransactionKind.Charge && item.ProviderPaymentId == paymentId)));
    }

    public Task<PlatformCardTransaction?> GetOpenChargeAsync(Guid platformInvoiceId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Transactions.FirstOrDefault(item => item.PlatformInvoiceId == platformInvoiceId && item.Kind == PlatformCardTransactionKind.Charge && item.IsOpen));
    }

    public Task<IReadOnlyList<PlatformCardTransaction>> ListForInvoiceAsync(Guid platformInvoiceId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PlatformCardTransaction>>(Transactions.Where(item => item.PlatformInvoiceId == platformInvoiceId).ToArray());

    public void AddTransaction(PlatformCardTransaction transaction)
    {
        if (transaction.Kind == PlatformCardTransactionKind.Charge && Transactions.Any(item => item.PlatformInvoiceId == transaction.PlatformInvoiceId && item.Kind == PlatformCardTransactionKind.Charge && item.IsOpen))
        {
            throw new InvalidOperationException("Unique index: one open charge per invoice.");
        }

        Transactions.Add(transaction);
    }

    public Task<PlatformPaymentLink?> GetLinkAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult(Links.FirstOrDefault(item => item.TokenHash == tokenHash));

    public Task<PlatformPaymentLink?> GetLinkByIdAsync(Guid linkId, CancellationToken cancellationToken) => Task.FromResult(Links.FirstOrDefault(item => item.Id == linkId));

    public void AddLink(PlatformPaymentLink link) => Links.Add(link);

    public Task<PlatformProviderEvent?> GetEventAsync(Guid recordId, CancellationToken cancellationToken) => Task.FromResult(Events.FirstOrDefault(item => item.Id == recordId));

    public Task<bool> EventSeenAsync(PlatformCardProvider provider, string providerEventId, CancellationToken cancellationToken) => Task.FromResult(Events.Any(item => item.Provider == provider && item.ProviderEventId == providerEventId));

    public void AddEvent(PlatformProviderEvent providerEvent) => Events.Add(providerEvent);

    public Task<PagedResult<PlatformCardTransactionItem>> ListTransactionsAsync(PlatformCardTransactionFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        PlatformCardTransactionItem[] items = Transactions
            .Where(item => filter.PlatformInvoiceId is null || item.PlatformInvoiceId == filter.PlatformInvoiceId)
            .Where(item => filter.Status is null || item.Status == filter.Status)
            .Select(item => new PlatformCardTransactionItem(item.Id, item.PlatformTenantId, "Tenant", item.PlatformInvoiceId, "INV", item.Kind, item.Origin, item.Status, item.Amount, item.CurrencyCode, item.FailureCode, item.Dispute, item.ProviderPaymentId, item.CreatedUtc, item.CompletedUtc, null))
            .ToArray();

        return Task.FromResult(new PagedResult<PlatformCardTransactionItem>(items, page, items.Length));
    }

    public Task<IReadOnlyList<Guid>> ListAutoChargeInvoicesAsync(DateTimeOffset utcNow, int take, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Guid>>(_billing.Invoices
            .Where(invoice => invoice.Status == PlatformInvoiceStatus.Issued && invoice.Balance > 0m && invoice.DueUtc <= utcNow)
            .Where(invoice => Profiles.Any(profile => profile.PlatformTenantId == invoice.PlatformTenantId && profile.AutoCharge))
            .Where(invoice => !Transactions.Any(item => item.PlatformInvoiceId == invoice.Id && item.Origin == PlatformCardTransactionOrigin.Automatic))
            .Select(invoice => invoice.Id)
            .Take(take)
            .ToArray());
    }

    public Task<IReadOnlyList<Guid>> ListStaleTransactionsAsync(DateTimeOffset createdBeforeUtc, int take, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Guid>>(Transactions.Where(item => item.IsOpen && (item.LastModifiedUtc ?? item.CreatedUtc) < createdBeforeUtc).Select(item => item.Id).Take(take).ToArray());
    }

    public Task<IReadOnlyList<Guid>> ListUnsentRefundsAsync(int take, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Guid>>(Transactions.Where(item => item.Kind == PlatformCardTransactionKind.Refund && item.Status == PlatformCardTransactionStatus.Created).Select(item => item.Id).Take(take).ToArray());
    }

    public Task SendSaveCardLinkAsync(PlatformTenant tenant, PlatformTenantContact recipient, string link, DateTimeOffset expiresUtc, CancellationToken cancellationToken)
    {
        Mailed.Add("save-card:" + recipient.Email.Value + ":" + link);
        return Task.CompletedTask;
    }

    public Task SendCardProblemAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformInvoice invoice, bool actionRequired, string? payLink, CancellationToken cancellationToken)
    {
        Mailed.Add((actionRequired ? "action:" : "failed:") + recipient.Email.Value + ":" + payLink);
        return Task.CompletedTask;
    }
}
