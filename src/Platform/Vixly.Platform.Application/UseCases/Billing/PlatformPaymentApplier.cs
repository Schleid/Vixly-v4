using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformPaymentApplier.cs
//
// PURPOSE:
// Records a payment on an invoice and everything that follows from it, for staff and for the card provider.
//
// RESPONSIBILITIES:
// - Take a receipt number, record the payment and its history and audit, store the receipt
// - When the invoice becomes paid, announce it and activate or clear the waiting subscription
//
// IMPORTANT NOTES:
// - One place for Slice 4 hand payments and Slice 5 card payments, so both behave the same
// - The caller saves
// =========================================================
public sealed class PlatformPaymentApplier
{
    private readonly PlatformBillingAdministration _billing;

    private readonly PlatformSubscriptionAdministration _subscriptions;

    public PlatformPaymentApplier(PlatformBillingAdministration billing, PlatformSubscriptionAdministration subscriptions)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
    }

    public async Task<PlatformInvoicePaymentResult> ApplyAsync(
        PlatformAuthorizationContext? context,
        PlatformInvoice invoice,
        decimal amount,
        PlatformPaymentMethod method,
        DateTimeOffset receivedUtc,
        string? reference,
        string? note,
        string? permissionCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        DateTimeOffset utcNow = _billing.UtcNow;
        string receipt = await _billing.NextNumberAsync(PlatformBillingDocumentKind.Receipt, cancellationToken);
        PlatformInvoicePaymentResult result = invoice.RecordPayment(receipt, amount, method, receivedUtc, reference, note, context?.PlatformUserId, utcNow);
        _billing.Record(context, invoice, result.Event, "platform.billing.payment_recorded", permissionCode);
        await _billing.StoreReceiptDocumentAsync(invoice, result.Payment, cancellationToken);

        if (result.BecamePaid)
        {
            _billing.AnnouncePaid(invoice);

            if (invoice.PlatformSubscriptionId is Guid subscriptionId
                && await _subscriptions.Subscriptions.GetAsync(subscriptionId, cancellationToken) is { } subscription
                && await _subscriptions.Plans.GetAsync(subscription.PlatformPlanId, cancellationToken) is { } plan
                && subscription.InvoicePaid(invoice.Number, await _billing.Invoices.HasOverdueAsync(subscriptionId, invoice.Id, utcNow, cancellationToken), utcNow) is { } paid)
            {
                _subscriptions.RecordChange(context, subscription, plan, paid, permissionCode, "platform.subscriptions.invoice_paid");
            }
        }

        return result;
    }
}
