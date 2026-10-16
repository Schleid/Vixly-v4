using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformBillingClock.cs
//
// PURPOSE:
// Marks overdue invoices past due, and their subscription Past due for an unpaid renewal or change (D-B8).
//
// RESPONSIBILITIES:
// - Find overdue invoices not yet marked
// - Mark each one, and its subscription when it bills a renewal or a change
//
// IMPORTANT NOTES:
// - Run by the subscription clock worker; one invoice per commit
// - Blocking for non-payment is Slice 6
// =========================================================
public sealed class PlatformBillingClock
{
    public const int BatchSize = 50;

    private readonly PlatformBillingAdministration _billing;

    private readonly PlatformSubscriptionAdministration _subscriptions;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public PlatformBillingClock(PlatformBillingAdministration billing, PlatformSubscriptionAdministration subscriptions, IPlatformUnitOfWork unitOfWork)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<int> ProcessOverdueAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = _billing.UtcNow;
        IReadOnlyList<Guid> overdue = await _billing.Invoices.ListOverdueUnmarkedAsync(utcNow, BatchSize, cancellationToken);
        int handled = 0;

        foreach (Guid invoiceId in overdue)
        {
            PlatformInvoice? invoice = await _billing.Invoices.GetAsync(invoiceId, cancellationToken);

            if (invoice?.MarkPastDueIfOverdue(utcNow) is not { } marked)
            {
                continue;
            }

            _billing.Record(null, invoice, marked, "platform.billing.invoice_past_due", null);

            if (invoice.Kind is PlatformInvoiceKind.Renewal or PlatformInvoiceKind.Change && invoice.PlatformSubscriptionId is Guid subscriptionId
                && await _subscriptions.Subscriptions.GetAsync(subscriptionId, cancellationToken) is { } subscription
                && await _subscriptions.Plans.GetAsync(subscription.PlatformPlanId, cancellationToken) is { } plan
                && subscription.MarkPastDue(invoice.Number, utcNow) is { } pastDue)
            {
                _subscriptions.RecordChange(null, subscription, plan, pastDue, null, "platform.subscriptions.past_due");
            }

            if (await _unitOfWork.SaveChangesAsync(cancellationToken))
            {
                handled++;
            }
            else
            {
                _unitOfWork.DiscardChanges();
            }
        }

        return handled;
    }
}
