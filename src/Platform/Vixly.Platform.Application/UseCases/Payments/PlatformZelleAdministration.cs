using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformZelleAdministration.cs
//
// PURPOSE:
// Shared steps of the Zelle use cases (Slice 5 update).
//
// RESPONSIBILITIES:
// - Load or create the Zelle details
// - Build the checkout offer for an invoice
// - Take a customer's report: one waiting per invoice, a few per invoice in all
// - Record a confirmed report as a Zelle payment through the normal payment rules
//
// IMPORTANT NOTES:
// - A report never pays an invoice; only staff confirmation does
// - Zelle is offered for open US-dollar invoices only
// =========================================================
public sealed class PlatformZelleAdministration
{
    private readonly PlatformPaymentsAdministration _payments;

    private readonly PlatformSubscriptionAdministration _subscriptions;

    private readonly IPlatformZelleRepository _store;

    private readonly IPlatformZelleQueries _queries;

    private readonly PlatformOutboxWriter _outbox;

    public PlatformZelleAdministration(
        PlatformPaymentsAdministration payments,
        PlatformSubscriptionAdministration subscriptions,
        IPlatformZelleRepository store,
        IPlatformZelleQueries queries,
        PlatformOutboxWriter outbox)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public DateTimeOffset UtcNow => _payments.UtcNow;

    public PlatformPaymentsAdministration Payments => _payments;

    public IPlatformZelleRepository Store => _store;

    public IPlatformZelleQueries Queries => _queries;

    public Task<Error?> AuthorizeAsync(PlatformAuthorizationContext context, string permissionCode, bool requireStepUp, CancellationToken cancellationToken)
    {
        return _payments.AuthorizeAsync(context, permissionCode, requireStepUp, cancellationToken);
    }

    public Task<bool> HoldsAsync(PlatformAuthorizationContext context, string permissionCode, CancellationToken cancellationToken)
    {
        return _payments.HoldsAsync(context, permissionCode, cancellationToken);
    }

    public Task<bool> SaveAsync(CancellationToken cancellationToken) => _payments.SaveAsync(cancellationToken);

    public async Task<PlatformZelleAccount> AccountAsync(CancellationToken cancellationToken)
    {
        PlatformZelleAccount? account = await _store.GetAccountAsync(cancellationToken);

        if (account is null)
        {
            account = PlatformZelleAccount.Create(UtcNow);
            _store.AddAccount(account);
        }

        return account;
    }

    // Null when Zelle is off, the invoice is not open, or it is not in US dollars.
    public async Task<PlatformZelleOffer?> OfferAsync(PlatformInvoice invoice, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (invoice.Status != PlatformInvoiceStatus.Issued || invoice.Balance <= 0m || !string.Equals(invoice.CurrencyCode, PlatformZelleLimits.Currency, StringComparison.Ordinal)
            || await _queries.GetAccountAsync(cancellationToken) is not { Enabled: true } account)
        {
            return null;
        }

        PlatformZelleClaim? waiting = await _store.FindWaitingAsync(invoice.Id, cancellationToken);
        int reports = await _store.CountForInvoiceAsync(invoice.Id, cancellationToken);

        return new PlatformZelleOffer(
            account.RecipientName,
            account.Email,
            account.Tag,
            account.Phone,
            account.HasQrCode ? account.QrCodeVersion : null,
            waiting is null ? null : new PlatformZelleWaitingClaim(waiting.Amount, waiting.SentOn, waiting.CreatedUtc),
            waiting is null && reports < PlatformZelleLimits.MaxClaimsPerInvoice);
    }

    public async Task<Error?> ReportAsync(PlatformInvoice invoice, PlatformZelleClaimSource source, decimal amount, DateTime? sentOn, string? senderName, string? reference, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (await OfferAsync(invoice, cancellationToken) is not { } offer)
        {
            return PlatformZelleErrors.NotAvailable;
        }

        if (offer.Waiting is not null)
        {
            return PlatformZelleErrors.ClaimWaiting;
        }

        if (!offer.CanReport)
        {
            return PlatformZelleErrors.TooManyClaims;
        }

        PlatformZelleClaim claim;

        try
        {
            claim = PlatformZelleClaim.Submit(invoice.PlatformTenantId, invoice.Id, invoice.Number, invoice.CurrencyCode, invoice.Balance, amount, sentOn, senderName, reference, source, UtcNow);
        }
        catch (DomainException exception)
        {
            return exception.Error;
        }

        _store.AddClaim(claim);
        Notice(claim, PlatformZelleNoticeKind.Received);
        _payments.Audit(null, "platform.payments.zelle_claim_submitted", null, nameof(PlatformZelleClaim), claim.Id, new { invoice = invoice.Number, amount = claim.Amount, source = claim.Source.ToString() });

        // A second report racing for the same invoice loses on the unique index.
        return await SaveAsync(cancellationToken) ? null : PlatformZelleErrors.ClaimWaiting;
    }

    public async Task<PlatformInvoicePaymentResult> RecordPaymentAsync(PlatformAuthorizationContext context, PlatformInvoice invoice, decimal amount, DateTimeOffset receivedUtc, string reference, string? note, CancellationToken cancellationToken)
    {
        return await new PlatformPaymentApplier(_payments.Billing, _subscriptions).ApplyAsync(
            context, invoice, amount, PlatformPaymentMethod.Zelle, receivedUtc, reference, note, PlatformPermissions.BillingRecordPayments, cancellationToken);
    }

    public void Notice(PlatformZelleClaim claim, PlatformZelleNoticeKind kind)
    {
        ArgumentNullException.ThrowIfNull(claim);
        _outbox.Write(PlatformMessageTypes.ZelleNotice, claim.PlatformTenantId, new PlatformZelleNotice(claim.PlatformTenantId, claim.Id, kind, UtcNow));
    }
}
