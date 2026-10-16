using Vixly.Platform.Application.Collections;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Collections;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformCollectionsNoticeHandler.cs
//
// PURPOSE:
// Sends a collections email: one per tenant and step, listing every overdue invoice with a pay link (D-C11).
//
// RESPONSIBILITIES:
// - Skip a reminder when the bill was settled before it went out
// - Issue and save pay links before mailing them
// - Send to the billing contact; the final warning and the block also to the owner
//
// IMPORTANT NOTES:
// - Payment instructions come from Billing settings in the tenant's language (D-C10)
// - Never sends a note or the internal reason of an extension
// =========================================================
public sealed class PlatformCollectionsNoticeHandler : IPlatformOutboxHandler
{
    private const int MaxInvoices = 10;

    private readonly PlatformCollectionsAdministration _collections;

    private readonly IPlatformCollectionsMailer _mailer;

    private readonly PlatformZelleAdministration _zelle;

    public PlatformCollectionsNoticeHandler(PlatformCollectionsAdministration collections, IPlatformCollectionsMailer mailer, PlatformZelleAdministration zelle)
    {
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
        _collections = collections ?? throw new ArgumentNullException(nameof(collections));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
    }

    public string MessageType => PlatformMessageTypes.CollectionsNotice;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformCollectionsNotice notice = PlatformOutboxWriter.Read<PlatformCollectionsNotice>(message);
        PlatformTenant? tenant = await _collections.Tenants.GetAsync(notice.PlatformTenantId, cancellationToken);

        if (tenant is null || tenant.Contacts.Count == 0)
        {
            return;
        }

        DateTimeOffset utcNow = _collections.UtcNow;
        IReadOnlyList<PlatformInvoice> overdue = notice.Kind == PlatformCollectionsNoticeKind.AccessRestored
            ? Array.Empty<PlatformInvoice>()
            : await _collections.Billing.Invoices.ListOverdueForTenantAsync(tenant.Id, utcNow, cancellationToken);

        // Paid before the email went out: nothing to remind about.
        if (notice.Kind != PlatformCollectionsNoticeKind.AccessRestored && overdue.Count == 0)
        {
            return;
        }

        List<PlatformCollectionsMailInvoice> invoices = new();
        bool linksIssued = false;

        foreach (PlatformInvoice invoice in overdue.OrderBy(item => item.DueUtc).Take(MaxInvoices))
        {
            string? token = null;

            // A pay link opens checkout: cards, or Zelle for US-dollar invoices.
            if (await _collections.Payments.RefusalForAsync(invoice.CurrencyCode, cancellationToken) is null || await _zelle.OfferAsync(invoice, cancellationToken) is not null)
            {
                (token, _) = _collections.Payments.IssueLink(PlatformPaymentLinkKind.PayInvoice, tenant.Id, invoice.Id, null);
                linksIssued = true;
            }

            invoices.Add(new PlatformCollectionsMailInvoice(invoice.Number, invoice.DueUtc, invoice.Balance, invoice.CurrencyCode, token));
        }

        // Saved before sending, so every link in the email works; not saved: the message is retried.
        if (linksIssued && !await _collections.Payments.UnitOfWork.SaveChangesAsync(cancellationToken))
        {
            throw new InvalidOperationException("The payment links could not be saved; the message will be retried.");
        }

        PlatformBillingSettings billing = await _collections.Billing.SettingsAsync(cancellationToken);
        PlatformCollectionsMail mail = new(
            notice.Kind,
            invoices.Select(item => item with { PayLink = item.PayLink is null ? null : _collections.Payments.Links.PayLink(item.PayLink) }).ToArray(),
            overdue.GroupBy(item => item.CurrencyCode).Select(group => new PlatformMoneyItem(group.Key, group.Sum(item => item.Balance))).OrderBy(item => item.CurrencyCode).ToArray(),
            notice.BlockDateUtc,
            notice.ExtendedThrough,
            billing.TextIn(tenant.DefaultLanguage)?.PaymentInstructions);

        PlatformTenantContact billingContact = tenant.ContactFor(PlatformTenantContactRole.Billing);
        await _mailer.SendAsync(tenant, billingContact, mail, cancellationToken);

        PlatformTenantContact owner = tenant.ContactFor(PlatformTenantContactRole.Owner);

        if (notice.Kind is PlatformCollectionsNoticeKind.FinalWarning or PlatformCollectionsNoticeKind.Blocked
            && !string.Equals(owner.Email.Value, billingContact.Email.Value, StringComparison.OrdinalIgnoreCase))
        {
            await _mailer.SendAsync(tenant, owner, mail, cancellationToken);
        }
    }
}
