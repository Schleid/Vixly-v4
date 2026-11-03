using System.Globalization;
using System.Text.Json;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.SalesFeed;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedRecorder.cs
//
// PURPOSE:
// Writes the sale events for TechOS from Vixly's invoice and subscription history (contract version 1).
//
// RESPONSIBILITIES:
// - Find history rows not yet in the feed, oldest first, across both journals
// - Freeze each as the exact JSON TechOS receives, with the next sequence number
// - Write the daily totals of each UTC day once, after that day has closed
//
// IMPORTANT NOTES:
// - The history rows are written in the same save as every sale, so no sale is missed and none is invented;
//   this job turns each into exactly one feed event (unique per source row)
// - The first run sends the whole history once, with each event's original time: that is the backfill
// - Events carry the original occurredUtc; sequence is the order they were written in
// - Events are never changed after they are written
// - Never card or bank details: only what the contract lists
// - invoice.issued carries the service period: a first-period invoice, issued before payment starts the period,
//   carries the period it will cover from its issue date (TechOS, 2026-11-03); a one-time charges invoice has none
// =========================================================
public sealed class PlatformSalesFeedRecorder
{
    // History rows read per journal and run.
    public const int BatchSize = 200;

    // Days totalled per run (a first run after a long pause catches up over several runs).
    public const int MaxTotalsDays = 31;

    // A day is totalled this long after it closed, so a sale saved at midnight is counted in its own day.
    public static TimeSpan TotalsDelay { get; } = TimeSpan.FromMinutes(10);

    private readonly IPlatformSalesFeedRepository _feed;

    private readonly IPlatformInvoiceRepository _invoices;

    private readonly IPlatformSubscriptionRepository _subscriptions;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformPlanRepository _plans;

    private readonly IPlatformPaymentsRepository _payments;

    private readonly IPlatformPublicLinks _links;

    public PlatformSalesFeedRecorder(
        IPlatformSalesFeedRepository feed,
        IPlatformInvoiceRepository invoices,
        IPlatformSubscriptionRepository subscriptions,
        IPlatformTenantRepository tenants,
        IPlatformPlanRepository plans,
        IPlatformPaymentsRepository payments,
        IPlatformPublicLinks links)
    {
        _feed = feed ?? throw new ArgumentNullException(nameof(feed));
        _invoices = invoices ?? throw new ArgumentNullException(nameof(invoices));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _links = links ?? throw new ArgumentNullException(nameof(links));
    }

    // =====================================================
    // METHOD: RECORD
    // Returns how many events were written and whether history rows are still waiting.
    // =====================================================
    public async Task<(int Written, bool More)> RecordAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        IReadOnlyList<PlatformInvoiceEvent> invoiceRows = await _feed.ListUnfedInvoiceEventsAsync(PlatformSalesFeedMapping.InvoiceCodes, BatchSize, cancellationToken);
        IReadOnlyList<PlatformSubscriptionEvent> subscriptionRows = await _feed.ListUnfedSubscriptionEventsAsync(PlatformSalesFeedMapping.SubscriptionCodes, BatchSize, cancellationToken);

        // Both journals in time order; when one list is full, stop where it stops so nothing older is left behind.
        DateTimeOffset? cut = null;

        if (invoiceRows.Count == BatchSize)
        {
            cut = invoiceRows[^1].CreatedUtc;
        }

        if (subscriptionRows.Count == BatchSize && (cut is null || subscriptionRows[^1].CreatedUtc < cut))
        {
            cut = subscriptionRows[^1].CreatedUtc;
        }

        List<(DateTimeOffset At, Guid Id, object Row)> rows = invoiceRows.Select(row => (row.CreatedUtc, row.Id, (object)row))
            .Concat(subscriptionRows.Select(row => (row.CreatedUtc, row.Id, (object)row)))
            .Where(item => cut is null || item.CreatedUtc <= cut)
            .OrderBy(item => item.CreatedUtc)
            .ThenBy(item => item.Id)
            .Select(item => (item.CreatedUtc, item.Id, item.Item3))
            .ToList();

        long sequence = await _feed.LastSequenceAsync(cancellationToken);

        foreach ((_, _, object row) in rows)
        {
            sequence++;
            _feed.Add(row is PlatformInvoiceEvent invoiceRow
                ? await InvoiceEventAsync(invoiceRow, sequence, utcNow, cancellationToken)
                : await SubscriptionEventAsync((PlatformSubscriptionEvent)row, sequence, utcNow, cancellationToken));
        }

        return (rows.Count, cut is not null);
    }

    // =====================================================
    // METHOD: REBUILD
    // A rejected event written again from its history row, as the event would be written today (for example with
    // a field the first version lacked), under a new id and the next sequence. Null when it cannot be rebuilt
    // (daily totals, or a history row that no longer exists).
    // =====================================================
    public async Task<PlatformSalesFeedEvent?> RebuildAsync(PlatformSalesFeedEvent rejected, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rejected);

        PlatformSalesFeedSource source = rejected.Source;
        Guid? sourceId = rejected.SourceId;

        // A correction points at the event it replaced; follow the chain back to the history row.
        for (int hops = 0; source == PlatformSalesFeedSource.Correction && sourceId is Guid previousId && hops < 20; hops++)
        {
            PlatformSalesFeedEvent? previous = await _feed.GetAsync(previousId, cancellationToken);

            if (previous is null)
            {
                return null;
            }

            source = previous.Source;
            sourceId = previous.SourceId;
        }

        if (sourceId is not Guid rowId)
        {
            return null;
        }

        long sequence = await _feed.LastSequenceAsync(cancellationToken) + 1;

        switch (source)
        {
            case PlatformSalesFeedSource.InvoiceEvent:
                return await _feed.FindInvoiceEventAsync(rowId, cancellationToken) is { } invoiceRow
                    ? await InvoiceEventAsync(invoiceRow, sequence, utcNow, cancellationToken, rejected.Id)
                    : null;
            case PlatformSalesFeedSource.SubscriptionEvent:
                return await _feed.FindSubscriptionEventAsync(rowId, cancellationToken) is { } subscriptionRow
                    ? await SubscriptionEventAsync(subscriptionRow, sequence, utcNow, cancellationToken, rejected.Id)
                    : null;
            default:
                return null;
        }
    }

    // =====================================================
    // METHOD: DAILY TOTALS
    // =====================================================
    public async Task<int> TotalsAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        DateOnly? first = await _feed.FirstSaleDayAsync(cancellationToken);

        if (first is null)
        {
            return 0;
        }

        DateOnly? last = await _feed.LastTotalsDayAsync(cancellationToken);
        DateOnly day = last is { } done && done >= first.Value ? done.AddDays(1) : first.Value;
        long sequence = await _feed.LastSequenceAsync(cancellationToken);
        int written = 0;

        while (written < MaxTotalsDays && Start(day.AddDays(1)) + TotalsDelay <= utcNow)
        {
            IReadOnlyList<PlatformSalesFeedDayItem> items = await _feed.ListDayAsync(Start(day), Start(day.AddDays(1)), cancellationToken);
            sequence++;
            DateOnly totalled = day;
            _feed.Add(PlatformSalesFeedEvent.Create(
                sequence,
                PlatformSalesFeedTypes.TotalsDaily,
                PlatformSalesFeedSource.DailyTotals,
                null,
                totalled,
                null,
                Start(totalled.AddDays(1)),
                null,
                null,
                id => PlatformSalesFeedJson.Event(id, PlatformSalesFeedTypes.TotalsDaily, Start(totalled.AddDays(1)), sequence, null, _links.ControlLink("/invoices"), writer => Totals(writer, totalled, items)),
                utcNow));
            written++;
            day = day.AddDays(1);
        }

        return written;
    }

    // {"date":"2026-11-02","types":{"invoice.issued":{"count":1,"sums":{"USD":"5400.00"}}}}; types without money give empty sums.
    public static void Totals(Utf8JsonWriter writer, DateOnly day, IReadOnlyList<PlatformSalesFeedDayItem> items)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(items);

        writer.WriteString("date", PlatformSalesFeedMapping.Date(day));
        writer.WriteStartObject("types");

        foreach (string type in PlatformSalesFeedTypes.Totalled)
        {
            PlatformSalesFeedDayItem[] ofType = items.Where(item => item.EventType == type).ToArray();

            if (ofType.Length == 0)
            {
                continue;
            }

            writer.WriteStartObject(type);
            writer.WriteNumber("count", ofType.Length);
            writer.WriteStartObject("sums");

            foreach (IGrouping<string, PlatformSalesFeedDayItem> currency in ofType.Where(item => item.Amount is not null && item.CurrencyCode is not null)
                .GroupBy(item => item.CurrencyCode!, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                writer.WriteString(currency.Key, PlatformSalesFeedMapping.Money(currency.Sum(item => item.Amount!.Value)));
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    // =====================================================
    // METHOD: SERVICE PERIOD
    // =====================================================
    // The period an invoice bills. A first-period invoice is issued before payment starts the subscription's period,
    // so it carries the period it will cover from its issue date, in the term it was priced for.
    public static (DateTimeOffset? Start, DateTimeOffset? End) ServicePeriod(PlatformInvoice invoice, PlatformSubscription? subscription)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (invoice.PeriodStartUtc is { } start && invoice.PeriodEndUtc is { } end)
        {
            return (start, end);
        }

        if (invoice.Kind != PlatformInvoiceKind.FirstPeriod)
        {
            return (invoice.PeriodStartUtc, invoice.PeriodEndUtc);
        }

        PlatformSubscriptionTerm? term = invoice.Lines
            .Where(line => line.Kind == PlatformInvoiceLineKind.PlanPeriod)
            .Select(line => line.Arguments.TryGetValue("term", out string? value) && Enum.TryParse(value, out PlatformSubscriptionTerm parsed) ? parsed : (PlatformSubscriptionTerm?)null)
            .FirstOrDefault(value => value is not null)
            ?? subscription?.Term;

        return term is { } priced
            ? (invoice.IssuedUtc, PlatformPricing.PeriodEnd(invoice.IssuedUtc, priced))
            : (invoice.PeriodStartUtc, invoice.PeriodEndUtc);
    }

    // =====================================================
    // METHOD: INVOICE EVENTS
    // =====================================================
    private async Task<PlatformSalesFeedEvent> InvoiceEventAsync(PlatformInvoiceEvent row, long sequence, DateTimeOffset utcNow, CancellationToken cancellationToken, Guid? replaces = null)
    {
        PlatformInvoice invoice = await _invoices.GetAsync(row.PlatformInvoiceId, cancellationToken)
            ?? throw new InvalidOperationException("Sales feed: invoice " + row.PlatformInvoiceId.ToString("D", CultureInfo.InvariantCulture) + " not found.");
        PlatformSalesFeedTenant tenant = await TenantAsync(invoice.PlatformTenantId, cancellationToken);
        string type = PlatformSalesFeedMapping.Type(row.EventCode);
        string controlUrl = _links.ControlLink("/invoices/" + invoice.Id.ToString("D", CultureInfo.InvariantCulture));
        Action<Utf8JsonWriter> data;
        decimal amount;

        switch (row.EventCode)
        {
            case PlatformInvoiceEventCodes.Issued:
            {
                PlatformSubscription? subscription = invoice.PlatformSubscriptionId is Guid subscriptionId ? await _subscriptions.GetAsync(subscriptionId, cancellationToken) : null;
                PlatformPlan? plan = subscription is null ? null : await _plans.GetAsync(subscription.PlatformPlanId, cancellationToken);
                (DateTimeOffset? periodStart, DateTimeOffset? periodEnd) = ServicePeriod(invoice, subscription);
                amount = invoice.Total;
                data = writer =>
                {
                    writer.WriteString("invoiceId", invoice.Id.ToString("D", CultureInfo.InvariantCulture));
                    writer.WriteString("number", invoice.Number);
                    writer.WriteString("issuedOn", PlatformSalesFeedMapping.Date(invoice.IssuedUtc));
                    writer.WriteString("dueOn", PlatformSalesFeedMapping.Date(invoice.DueUtc));
                    PlatformSalesFeedJson.Text(writer, "periodStart", periodStart is { } start ? PlatformSalesFeedMapping.Date(start) : null);
                    PlatformSalesFeedJson.Text(writer, "periodEnd", periodEnd is { } end ? PlatformSalesFeedMapping.Date(end) : null);
                    writer.WriteString("currency", invoice.CurrencyCode);
                    writer.WriteString("subtotal", PlatformSalesFeedMapping.Money(invoice.NetTotal - invoice.DiscountTotal));
                    writer.WriteString("tax", PlatformSalesFeedMapping.Money(invoice.TaxTotal));
                    writer.WriteString("total", PlatformSalesFeedMapping.Money(invoice.Total));
                    PlatformSalesFeedJson.Text(writer, "planCode", plan?.Code);

                    if (subscription is null)
                    {
                        writer.WriteNull("licences");
                    }
                    else
                    {
                        writer.WriteNumber("licences", subscription.Licences);
                    }
                };
                break;
            }

            case PlatformInvoiceEventCodes.PaymentRecorded:
            {
                PlatformPayment payment = Payment(invoice, row);
                string? wallet = payment.Method == PlatformPaymentMethod.Card && payment.Reference is { } reference
                    ? (await _payments.FindTransactionAsync(null, reference, null, cancellationToken))?.Wallet
                    : null;
                amount = payment.Amount;
                data = writer =>
                {
                    writer.WriteString("paymentId", payment.Id.ToString("D", CultureInfo.InvariantCulture));
                    writer.WriteString("invoiceNumber", invoice.Number);
                    writer.WriteString("amount", PlatformSalesFeedMapping.Money(payment.Amount));
                    writer.WriteString("currency", invoice.CurrencyCode);
                    writer.WriteString("method", PlatformSalesFeedMapping.Method(payment.Method, wallet));

                    // Card and bank: recorded when it settled; Zelle and manual: when staff confirmed it.
                    writer.WriteString("receivedOn", PlatformSalesFeedMapping.Date(row.CreatedUtc));
                };
                break;
            }

            case PlatformInvoiceEventCodes.PaymentReversed:
            case PlatformInvoiceEventCodes.DisputeLost:
            case PlatformInvoiceEventCodes.CardRefunded:
            {
                PlatformPayment payment = row.EventCode == PlatformInvoiceEventCodes.CardRefunded
                    ? await RefundedPaymentAsync(invoice, row, cancellationToken)
                    : Payment(invoice, row);
                amount = ArgumentAmount(row);
                string reason = PlatformSalesFeedMapping.Reason(row.EventCode, payment.Method);
                data = writer =>
                {
                    writer.WriteString("paymentId", payment.Id.ToString("D", CultureInfo.InvariantCulture));
                    writer.WriteString("invoiceNumber", invoice.Number);
                    writer.WriteString("amount", PlatformSalesFeedMapping.Money(amount));
                    writer.WriteString("currency", invoice.CurrencyCode);
                    writer.WriteString("reason", reason);

                    // Vixly's own record of this reversal: stable and unique, so partial refunds stay separate.
                    writer.WriteString("reversalId", row.Id.ToString("D", CultureInfo.InvariantCulture));
                };
                break;
            }

            default:
            {
                amount = ArgumentAmount(row);
                string? creditNote = row.EventCode == PlatformInvoiceEventCodes.CreditIssued ? row.Arguments.GetValueOrDefault("number") : null;
                bool credited = row.EventCode == PlatformInvoiceEventCodes.CreditIssued;
                data = writer =>
                {
                    writer.WriteString("invoiceNumber", invoice.Number);
                    writer.WriteString("amount", PlatformSalesFeedMapping.Money(amount));
                    writer.WriteString("currency", invoice.CurrencyCode);

                    if (credited)
                    {
                        PlatformSalesFeedJson.Text(writer, "creditNoteNumber", creditNote);
                    }
                };
                break;
            }
        }

        return PlatformSalesFeedEvent.Create(
            sequence,
            type,
            replaces is null ? PlatformSalesFeedSource.InvoiceEvent : PlatformSalesFeedSource.Correction,
            replaces ?? row.Id,
            null,
            invoice.PlatformTenantId,
            row.CreatedUtc,
            Math.Abs(amount),
            invoice.CurrencyCode,
            id => PlatformSalesFeedJson.Event(id, type, row.CreatedUtc, sequence, tenant, controlUrl, data),
            utcNow);
    }

    // =====================================================
    // METHOD: SUBSCRIPTION EVENTS
    // =====================================================
    private async Task<PlatformSalesFeedEvent> SubscriptionEventAsync(PlatformSubscriptionEvent row, long sequence, DateTimeOffset utcNow, CancellationToken cancellationToken, Guid? replaces = null)
    {
        PlatformSubscription subscription = await _subscriptions.GetAsync(row.PlatformSubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("Sales feed: subscription " + row.PlatformSubscriptionId.ToString("D", CultureInfo.InvariantCulture) + " not found.");
        PlatformSalesFeedTenant tenant = await TenantAsync(row.PlatformTenantId, cancellationToken);
        PlatformPlan current = await _plans.GetAsync(subscription.PlatformPlanId, cancellationToken)
            ?? throw new InvalidOperationException("Sales feed: plan not found.");

        // What the event itself recorded (plan, version, term, licences) wins over today's subscription.
        IReadOnlyDictionary<string, string> arguments = row.Arguments;
        PlatformPlan plan = arguments.TryGetValue("plan", out string? code) && code != current.Code
            ? await _plans.GetByCodeAsync(code, cancellationToken) ?? current
            : current;
        PlatformPlanVersion version = arguments.TryGetValue("version", out string? number)
            && int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out int versionNumber)
            && plan.Versions.FirstOrDefault(item => item.Number == versionNumber) is { } recorded
                ? recorded
                : plan.Id == subscription.PlatformPlanId ? plan.Version(subscription.PlanVersionId) : plan.Current ?? plan.Version(subscription.PlanVersionId);
        PlatformSubscriptionTerm term = arguments.TryGetValue("term", out string? termName) && Enum.TryParse(termName, out PlatformSubscriptionTerm parsedTerm) ? parsedTerm : subscription.Term;
        int licences = arguments.TryGetValue("licences", out string? count) && int.TryParse(count, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedLicences) ? parsedLicences : subscription.Licences;
        decimal perPeriod = PlatformPricing.PeriodAmount(version, term, licences);
        string change = PlatformSalesFeedMapping.Change(row.EventCode, arguments)
            ?? throw new InvalidOperationException("Sales feed: no change word for " + row.EventCode + ".");
        DateTimeOffset? renews = row.ToStatus == PlatformSubscriptionStatus.Ended || subscription.Status == PlatformSubscriptionStatus.Ended || subscription.CancelAtPeriodEnd
            ? null
            : arguments.TryGetValue("periodEnd", out string? periodEnd) && DateTimeOffset.TryParse(periodEnd, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset parsedEnd)
                ? parsedEnd
                : subscription.PeriodEndUtc;
        string controlUrl = _links.ControlLink("/tenants/" + row.PlatformTenantId.ToString("D", CultureInfo.InvariantCulture));

        return PlatformSalesFeedEvent.Create(
            sequence,
            PlatformSalesFeedTypes.SubscriptionChanged,
            replaces is null ? PlatformSalesFeedSource.SubscriptionEvent : PlatformSalesFeedSource.Correction,
            replaces ?? row.Id,
            null,
            row.PlatformTenantId,
            row.CreatedUtc,
            null,
            null,
            id => PlatformSalesFeedJson.Event(id, PlatformSalesFeedTypes.SubscriptionChanged, row.CreatedUtc, sequence, tenant, controlUrl, writer =>
            {
                writer.WriteString("subscriptionId", subscription.Id.ToString("D", CultureInfo.InvariantCulture));
                writer.WriteString("change", change);
                writer.WriteString("planCode", plan.Code);
                writer.WriteString("planName", plan.NameIn(LanguageCode.En));
                writer.WriteNumber("licences", licences);
                writer.WriteString("term", PlatformSalesFeedMapping.Term(term));
                writer.WriteString("amountPerPeriod", PlatformSalesFeedMapping.Money(perPeriod));
                writer.WriteString("currency", version.CurrencyCode);
                writer.WriteString("status", PlatformSalesFeedMapping.Status(row.ToStatus));
                PlatformSalesFeedJson.Text(writer, "renewsOn", renews is { } at ? PlatformSalesFeedMapping.Date(at) : null);
            }),
            utcNow);
    }

    private async Task<PlatformSalesFeedTenant> TenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        PlatformTenant tenant = await _tenants.GetAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Sales feed: tenant " + tenantId.ToString("D", CultureInfo.InvariantCulture) + " not found.");
        return PlatformSalesFeedTenant.From(tenant);
    }

    private static PlatformPayment Payment(PlatformInvoice invoice, PlatformInvoiceEvent row)
    {
        string? receipt = row.Arguments.GetValueOrDefault("receipt");
        return invoice.Payments.FirstOrDefault(item => item.ReceiptNumber == receipt)
            ?? throw new InvalidOperationException("Sales feed: payment " + receipt + " not found on " + invoice.Number + ".");
    }

    // A card refund names the provider's refund id: its transaction leads to the charge and so to the payment.
    private async Task<PlatformPayment> RefundedPaymentAsync(PlatformInvoice invoice, PlatformInvoiceEvent row, CancellationToken cancellationToken)
    {
        if (row.Arguments.GetValueOrDefault("reference") is { Length: > 0 } reference
            && await _payments.FindTransactionAsync(null, null, reference, cancellationToken) is { ChargeTransactionId: Guid chargeId }
            && await _payments.GetTransactionAsync(chargeId, cancellationToken) is { PlatformPaymentId: Guid paymentId }
            && invoice.Payments.FirstOrDefault(item => item.Id == paymentId) is { } linked)
        {
            return linked;
        }

        return invoice.Payments.Where(item => item.Method is PlatformPaymentMethod.Card or PlatformPaymentMethod.BankDebit).OrderByDescending(item => item.ReceivedUtc).FirstOrDefault()
            ?? invoice.Payments.OrderByDescending(item => item.ReceivedUtc).FirstOrDefault()
            ?? throw new InvalidOperationException("Sales feed: no payment for the refund on " + invoice.Number + ".");
    }

    private static decimal ArgumentAmount(PlatformInvoiceEvent row)
    {
        return decimal.Parse(row.Arguments.GetValueOrDefault("amount") ?? "0", NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset Start(DateOnly day)
    {
        return new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }
}
