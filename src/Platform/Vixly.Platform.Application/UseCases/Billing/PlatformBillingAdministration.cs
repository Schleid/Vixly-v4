using System.Globalization;
using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformBillingAdministration.cs
//
// PURPOSE:
// Shared steps of the billing use cases: authorization, settings, numbers, tax, documents and records (Slice 4).
//
// RESPONSIBILITIES:
// - Check the permission (and step-up) first
// - Hand out gap-free numbers inside the caller's commit (D-B2)
// - Pick the tax rate for a tenant (D-B1) and freeze the customer on the invoice
// - Store a rendered document and queue its email (D-B10, D-B13)
//
// IMPORTANT NOTES:
// - Scoped: one instance per use case, so numbers taken in one commit are remembered
// - Never commits: the use case saves once
// =========================================================
public sealed class PlatformBillingAdministration
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformPermissionReader _permissions;

    private readonly IPlatformInvoiceRepository _invoices;

    private readonly IPlatformBillingRepository _billing;

    private readonly IPlatformBillingDocuments _documents;

    private readonly PlatformAuditTrail _audit;

    private readonly PlatformOutboxWriter _outbox;

    private readonly TimeProvider _timeProvider;

    public PlatformBillingAdministration(
        IPlatformAuthorizationGate gate,
        IPlatformStepUpGuard stepUp,
        IPlatformPermissionReader permissions,
        IPlatformInvoiceRepository invoices,
        IPlatformBillingRepository billing,
        IPlatformBillingDocuments documents,
        PlatformAuditTrail audit,
        PlatformOutboxWriter outbox,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _invoices = invoices ?? throw new ArgumentNullException(nameof(invoices));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public IPlatformInvoiceRepository Invoices => _invoices;

    public IPlatformBillingRepository Store => _billing;

    public PlatformOutboxWriter Outbox => _outbox;

    // =====================================================
    // METHOD: AUTHORIZE
    // =====================================================
    public async Task<Error?> AuthorizeAsync(PlatformAuthorizationContext context, string permissionCode, bool requireStepUp, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        await _gate.AuthorizeAsync(context, permissionCode, cancellationToken);

        return requireStepUp && !await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken)
            ? PlatformCommonErrors.StepUpRequired
            : null;
    }

    public async Task<bool> HoldsAsync(PlatformAuthorizationContext context, string permissionCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        PlatformPermissionSnapshot? actor = await _permissions.ReadAsync(context.PlatformUserId, UtcNow, cancellationToken);

        return actor is not null && actor.IsAccountActive && PlatformAccess.Holds(actor, permissionCode);
    }

    // =====================================================
    // METHOD: SETTINGS AND NUMBERS (D-B2, D-B4)
    // =====================================================
    // Read each time, never cached here: in the console this service lives as long as the browser
    // circuit, so a cached copy would go stale after a save or a discarded change.
    public async Task<PlatformBillingSettings> SettingsAsync(CancellationToken cancellationToken)
    {
        return await _billing.GetSettingsAsync(cancellationToken) ?? PlatformBillingSettings.Create(UtcNow);
    }

    public async Task<string> NextNumberAsync(PlatformBillingDocumentKind kind, CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = UtcNow;
        int year = utcNow.Year;

        PlatformNumberSequence? sequence = await _billing.GetSequenceAsync(kind, year, cancellationToken);

        if (sequence is null)
        {
            sequence = PlatformNumberSequence.Start(kind, year, utcNow);
            _billing.AddSequence(sequence);
        }

        PlatformBillingSettings settings = await SettingsAsync(cancellationToken);

        return sequence.Take(settings.PrefixFor(kind), utcNow);
    }

    // =====================================================
    // METHOD: TAX AND CUSTOMER (D-B1, D-B16)
    // =====================================================
    public async Task<(decimal Rate, string? Label)> TaxForAsync(PlatformTenant tenant, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        if (tenant.TaxExempt)
        {
            return (0m, null);
        }

        PlatformPostalAddress address = tenant.Company.Address;
        IReadOnlyList<PlatformTaxRate> rates = await _billing.ListTaxRatesAsync(address.CountryCode, cancellationToken);
        PlatformTaxRate? rate = PlatformTaxRate.Pick(rates, address.CountryCode, address.StateProvince, UtcNow);

        return rate is null ? (0m, null) : (rate.Rate, rate.Label);
    }

    public static PlatformInvoiceParty Customer(PlatformTenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        PlatformPostalAddress address = tenant.Company.Address;
        string place = string.Join(" ", new[] { address.PostalCode, address.City, address.StateProvince }.Where(part => !string.IsNullOrWhiteSpace(part)));
        string lines = string.Join("\n", new[] { address.Line1, address.Line2, place, address.CountryCode }.Where(part => !string.IsNullOrWhiteSpace(part)));
        string email = tenant.Contacts.Count > 0 ? tenant.ContactFor(PlatformTenantContactRole.Billing).Email.Value : tenant.Company.Email.Value;

        return PlatformInvoiceParty.Create(tenant.Company.LegalName, lines, tenant.TaxId, email);
    }

    // =====================================================
    // METHOD: DOCUMENTS (D-B10, D-B13)
    // =====================================================
    public async Task<PlatformCompanyProfile> IssuerAsync(CancellationToken cancellationToken)
    {
        return await _billing.GetProfileAsync(cancellationToken)
            ?? throw new Vixly.Shared.Kernel.Domain.DomainException(PlatformBillingErrors.ProfileMissing);
    }

    public async Task StoreInvoiceDocumentAsync(PlatformInvoice invoice, CancellationToken cancellationToken)
    {
        PlatformCompanyProfile issuer = await IssuerAsync(cancellationToken);
        PlatformBillingSettings settings = await SettingsAsync(cancellationToken);
        byte[] pdf = _documents.RenderInvoice(invoice, issuer, settings);
        await StoreAsync(PlatformBillingDocumentKind.Invoice, invoice, invoice.Id, invoice.Number, pdf, cancellationToken);
    }

    public async Task StoreCreditDocumentAsync(PlatformInvoice invoice, PlatformCreditNote credit, CancellationToken cancellationToken)
    {
        PlatformCompanyProfile issuer = await IssuerAsync(cancellationToken);
        PlatformBillingSettings settings = await SettingsAsync(cancellationToken);
        byte[] pdf = _documents.RenderCreditNote(invoice, credit, issuer, settings);
        await StoreAsync(PlatformBillingDocumentKind.CreditNote, invoice, credit.Id, credit.Number!, pdf, cancellationToken);
    }

    public async Task StoreReceiptDocumentAsync(PlatformInvoice invoice, PlatformPayment payment, CancellationToken cancellationToken)
    {
        PlatformCompanyProfile issuer = await IssuerAsync(cancellationToken);
        PlatformBillingSettings settings = await SettingsAsync(cancellationToken);
        byte[] pdf = _documents.RenderReceipt(invoice, payment, issuer, settings);
        await StoreAsync(PlatformBillingDocumentKind.Receipt, invoice, payment.Id, payment.ReceiptNumber, pdf, cancellationToken);
    }

    private async Task StoreAsync(PlatformBillingDocumentKind kind, PlatformInvoice invoice, Guid ownerId, string number, byte[] pdf, CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = UtcNow;
        _billing.AddDocument(PlatformBillingDocument.Create(kind, invoice.Id, ownerId, number, invoice.Language, pdf, utcNow));

        if ((await SettingsAsync(cancellationToken)).EmailDocuments)
        {
            _outbox.Write(
                PlatformMessageTypes.BillingDocumentIssued,
                invoice.PlatformTenantId,
                new PlatformBillingDocumentIssued(kind, invoice.Id, ownerId, invoice.PlatformTenantId, utcNow));
        }
    }

    // =====================================================
    // METHOD: RECORD (D-2)
    // =====================================================
    public void Record(PlatformAuthorizationContext? context, PlatformInvoice invoice, PlatformInvoiceEvent change, string actionCode, string? permissionCode)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(change);

        _invoices.AddEvents(new[] { change });
        _audit.Record(
            context?.PlatformUserId,
            actionCode,
            permissionCode,
            nameof(PlatformInvoice),
            invoice.Id,
            arguments: change.Arguments,
            after: new { number = invoice.Number, status = invoice.Status.ToString(), balance = invoice.Balance.ToString("0.00", CultureInfo.InvariantCulture) },
            reason: change.Reason);
    }

    public void Audit(PlatformAuthorizationContext context, string actionCode, string permissionCode, string targetType, Guid? targetId, object? after = null, string? reason = null)
    {
        _audit.Record(context?.PlatformUserId, actionCode, permissionCode, targetType, targetId, after: after, reason: reason);
    }

    public void AnnouncePaid(PlatformInvoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        _outbox.Write(
            PlatformMessageTypes.InvoicePaid,
            invoice.PlatformTenantId,
            new PlatformInvoicePaid(invoice.Id, invoice.Number, invoice.PlatformTenantId, invoice.PlatformSubscriptionId, invoice.Total, invoice.CurrencyCode, UtcNow));
    }

    public void RequestApproval(PlatformInvoice invoice, Guid requestedByUserId)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        _outbox.Write(
            PlatformMessageTypes.ApprovalRequested,
            invoice.Id,
            new PlatformApprovalRequested(PlatformApprovalKind.CreditNote, invoice.Id, PlatformPermissions.BillingApproveCredits, invoice.Number, requestedByUserId, UtcNow));
    }
}
