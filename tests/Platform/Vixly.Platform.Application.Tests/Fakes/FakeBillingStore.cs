using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeBillingStore.cs
//
// PURPOSE:
// In-memory billing ports for the Slice 4 use-case tests.
//
// RESPONSIBILITIES:
// - Hold invoices, history, settings, profile, tax rates, sequences and documents
// - Render "PDFs" as small byte arrays and record emails
//
// IMPORTANT NOTES:
// - Test double only; no persistence rules beyond what the tests need
// =========================================================
internal sealed class FakeBillingStore :
    IPlatformInvoiceRepository,
    IPlatformInvoiceHistory,
    IPlatformBillingRepository,
    IPlatformBillingDocuments,
    IPlatformBillingMailer,
    IPlatformBillingQueries
{
    public List<PlatformInvoice> Invoices { get; } = new();

    public List<PlatformInvoiceEvent> Events { get; } = new();

    public List<PlatformTaxRate> Rates { get; } = new();

    public List<PlatformNumberSequence> Sequences { get; } = new();

    public List<PlatformBillingDocument> Documents { get; } = new();

    public List<string> Mailed { get; } = new();

    public PlatformBillingSettings? Settings { get; set; }

    public PlatformCompanyProfile? Profile { get; set; }

    public Task<PlatformInvoice?> GetAsync(Guid invoiceId, CancellationToken cancellationToken) => Task.FromResult(Invoices.FirstOrDefault(item => item.Id == invoiceId));

    public Task<bool> SourceBilledAsync(string sourceKey, CancellationToken cancellationToken) => Task.FromResult(Invoices.Any(item => item.SourceKey == sourceKey));

    public Task<IReadOnlyList<Guid>> ListOverdueUnmarkedAsync(DateTimeOffset utcNow, int take, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Guid>>(Invoices.Where(item => item.IsOverdueAt(utcNow) && item.PastDueUtc is null).Select(item => item.Id).Take(take).ToArray());
    }

    public Task<IReadOnlyList<PlatformInvoice>> ListOverdueForTenantAsync(Guid platformTenantId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformInvoice>>(Invoices
            .Where(item => item.PlatformTenantId == platformTenantId && item.IsOverdueAt(utcNow) && item.Balance > 0m)
            .OrderBy(item => item.DueUtc)
            .ToArray());
    }

    public Task<bool> HasOverdueAsync(Guid platformSubscriptionId, Guid exceptInvoiceId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        return Task.FromResult(Invoices.Any(item => item.PlatformSubscriptionId == platformSubscriptionId && item.Id != exceptInvoiceId && item.IsOverdueAt(utcNow)));
    }

    public Task<Guid?> FindOpenFirstPeriodAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Invoices
            .Where(item => item.PlatformTenantId == platformTenantId && item.Kind == PlatformInvoiceKind.FirstPeriod && item.Status == PlatformInvoiceStatus.Issued)
            .OrderByDescending(item => item.IssuedUtc)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefault());
    }

    public void Add(PlatformInvoice invoice) => Invoices.Add(invoice);

    public void AddEvents(IEnumerable<PlatformInvoiceEvent> events) => Events.AddRange(events);

    public Task<IReadOnlyList<PlatformInvoiceEvent>> ListAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformInvoiceEvent>>(Events.Where(item => item.PlatformInvoiceId == invoiceId).ToArray());
    }

    public Task<PlatformBillingSettings?> GetSettingsAsync(CancellationToken cancellationToken) => Task.FromResult(Settings);

    public void AddSettings(PlatformBillingSettings settings) => Settings = settings;

    public Task<PlatformCompanyProfile?> GetProfileAsync(CancellationToken cancellationToken) => Task.FromResult(Profile);

    public void AddProfile(PlatformCompanyProfile profile) => Profile = profile;

    public Task<IReadOnlyList<PlatformTaxRate>> ListTaxRatesAsync(string? countryCode, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformTaxRate>>(Rates.Where(rate => countryCode is null || rate.CountryCode == countryCode).ToArray());
    }

    public void AddTaxRate(PlatformTaxRate rate) => Rates.Add(rate);

    public Task<PlatformNumberSequence?> GetSequenceAsync(PlatformBillingDocumentKind kind, int year, CancellationToken cancellationToken)
    {
        return Task.FromResult(Sequences.FirstOrDefault(item => item.Kind == kind && item.Year == year));
    }

    public void AddSequence(PlatformNumberSequence sequence) => Sequences.Add(sequence);

    public Task<PlatformBillingDocument?> GetDocumentAsync(PlatformBillingDocumentKind kind, Guid ownerId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Documents.FirstOrDefault(item => item.Kind == kind && item.OwnerId == ownerId));
    }

    public void AddDocument(PlatformBillingDocument document) => Documents.Add(document);

    public byte[] RenderInvoice(PlatformInvoice invoice, PlatformCompanyProfile issuer, PlatformBillingSettings settings) => new byte[] { 1, 2, 3 };

    public byte[] RenderCreditNote(PlatformInvoice invoice, PlatformCreditNote credit, PlatformCompanyProfile issuer, PlatformBillingSettings settings) => new byte[] { 4, 5, 6 };

    public byte[] RenderReceipt(PlatformInvoice invoice, PlatformPayment payment, PlatformCompanyProfile issuer, PlatformBillingSettings settings) => new byte[] { 7, 8, 9 };

    public List<string?> PayLinks { get; } = new();

    public Task SendDocumentAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformInvoice invoice, PlatformBillingDocument document, string? payLink, CancellationToken cancellationToken)
    {
        PayLinks.Add(payLink);
        Mailed.Add(recipient.Email.Value + ":" + document.Number);
        return Task.CompletedTask;
    }

    public Task<PagedResult<PlatformInvoiceListItem>> ListInvoicesAsync(PlatformInvoiceListFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        PlatformInvoiceListItem[] items = Invoices
            .Where(item => filter.PlatformTenantId is null || item.PlatformTenantId == filter.PlatformTenantId)
            .Select(item => new PlatformInvoiceListItem(item.Id, item.Number, item.PlatformTenantId, item.Customer.Name, item.Kind, item.Status, item.CurrencyCode, item.Total, item.Balance, item.PaidTotal, item.IssuedUtc, item.DueUtc, item.IsOverdueAt(filter.UtcNow), item.PendingCredit is not null))
            .ToArray();

        return Task.FromResult(new PagedResult<PlatformInvoiceListItem>(items, page, items.Length));
    }

    public Task<PagedResult<PlatformBillingDocumentItem>> ListDocumentsAsync(Guid platformTenantId, PagedRequest page, CancellationToken cancellationToken)
    {
        PlatformBillingDocumentItem[] items = Documents
            .Join(Invoices, document => document.PlatformInvoiceId, invoice => invoice.Id, (document, invoice) => (document, invoice))
            .Where(row => row.invoice.PlatformTenantId == platformTenantId)
            .OrderByDescending(row => row.document.CreatedUtc)
            .Select(row => new PlatformBillingDocumentItem(row.document.Kind, row.document.OwnerId, row.document.Number, row.invoice.Id, row.invoice.Number, row.document.Language.Value, row.document.CreatedUtc))
            .ToArray();

        return Task.FromResult(new PagedResult<PlatformBillingDocumentItem>(items, page, items.Length));
    }

    public Task<IReadOnlyList<PlatformTenantBillingSummary>> SummaryAsync(Guid platformTenantId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformTenantBillingSummary>>(Array.Empty<PlatformTenantBillingSummary>());
    }
}
