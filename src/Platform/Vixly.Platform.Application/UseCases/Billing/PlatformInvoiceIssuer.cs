using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformInvoiceIssuer.cs
//
// PURPOSE:
// Builds, numbers, renders and records one invoice in the caller's commit (D-B2, D-B3, D-B10).
//
// RESPONSIBILITIES:
// - Apply the tenant's tax to each line
// - Take the next number and issue the invoice
// - Store its PDF, queue its email, write history and audit
//
// IMPORTANT NOTES:
// - Refuses (ProfileMissing) until the company profile exists: an invoice without its issuer is not valid
// - The caller commits; a failed commit leaves no number used
// =========================================================
public sealed class PlatformInvoiceIssuer
{
    private readonly PlatformBillingAdministration _billing;

    public PlatformInvoiceIssuer(PlatformBillingAdministration billing)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
    }

    public async Task<PlatformInvoice> IssueAsync(
        PlatformTenant tenant,
        Guid? platformSubscriptionId,
        PlatformInvoiceKind kind,
        string sourceKey,
        string currencyCode,
        DateTimeOffset? periodStartUtc,
        DateTimeOffset? periodEndUtc,
        IReadOnlyList<PlatformInvoiceDraftLine> lines,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(lines);

        await _billing.IssuerAsync(cancellationToken);
        PlatformBillingSettings settings = await _billing.SettingsAsync(cancellationToken);
        (decimal rate, string? label) = await _billing.TaxForAsync(tenant, cancellationToken);
        PlatformInvoiceLineInput[] taxed = lines
            .Select(line => new PlatformInvoiceLineInput(line.Kind, line.TextKey, line.Arguments, line.Quantity, line.UnitPrice, rate, label, line.SourceId))
            .ToArray();
        string number = await _billing.NextNumberAsync(PlatformBillingDocumentKind.Invoice, cancellationToken);

        PlatformInvoiceIssue issue = PlatformInvoice.Issue(
            tenant.Id,
            platformSubscriptionId,
            kind,
            number,
            sourceKey,
            currencyCode,
            tenant.DefaultLanguage,
            PlatformBillingAdministration.Customer(tenant),
            settings.DueDays,
            periodStartUtc,
            periodEndUtc,
            taxed,
            _billing.UtcNow);

        _billing.Invoices.Add(issue.Invoice);
        _billing.Record(null, issue.Invoice, issue.Event, "platform.billing.invoice_issued", null);
        await _billing.StoreInvoiceDocumentAsync(issue.Invoice, cancellationToken);

        return issue.Invoice;
    }
}
