using System.Globalization;
using System.Text.Json;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.SalesFeed;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.SalesFeed;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformSalesFeedTests.cs
//
// PURPOSE:
// Proves the sales feed to TechOS (contract version 1, 2026-11-02).
//
// RESPONSIBILITIES:
// - Each history row becomes exactly one event, in time order, with its original time (the backfill)
// - The exact JSON of every event type; status, method, change and reason mapping
// - The sender's answers: accepted, duplicate, rejected (parked), 413, 400/401, 404/5xx and the 24-hour alert
// - The daily totals, written once after the day closed
//
// IMPORTANT NOTES:
// - In-memory stores; the database rules are proved in Infrastructure tests
// =========================================================
public sealed class PlatformSalesFeedTests
{
    private static readonly Guid Actor = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private readonly PaymentsTestKit _kit = new();

    private readonly FakeSalesFeedStore _feed;

    public PlatformSalesFeedTests()
    {
        _feed = new FakeSalesFeedStore(_kit.Billing.Events, _kit.Kit.Store.Events);
    }

    private DateTimeOffset Now => _kit.Now;

    // =====================================================
    // Galeo's history: subscription started and paid, invoice issued and paid
    // =====================================================
    private sealed record History(PlatformTenant Tenant, PlatformPlan Plan, PlatformSubscription Subscription, PlatformInvoice Invoice, PlatformPayment Payment);

    private History Galeo(PlatformPaymentMethod method = PlatformPaymentMethod.BankTransfer)
    {
        PlatformPlan plan = _kit.Kit.Plan("PRO", 55m);
        PlatformTenant tenant = _kit.Kit.Tenants.Tenant("galeo", active: true);
        PlatformSubscriptionStart start = PlatformSubscription.Start(tenant.Id, plan, PlatformSubscriptionTerm.Annual, 10, 10, false, null, Now.AddDays(-20));
        _kit.Kit.Store.Subscriptions.Add(start.Subscription);
        _kit.Kit.Store.Events.Add(start.Event);
        PlatformInvoice invoice = Issue(tenant, start.Subscription, "INV-2026-000001", 5400m, Now.AddDays(-19));
        PlatformInvoicePaymentResult paid = invoice.RecordPayment("RCT-2026-000001", 5400m, method, Now.AddDays(-18), "WIRE-1", null, Actor, Now.AddDays(-18));
        _kit.Billing.Events.Add(paid.Event);
        _kit.Kit.Store.Events.Add(start.Subscription.RecordOfflinePayment("Wire", Actor, Now.AddDays(-17)));
        return new History(tenant, plan, start.Subscription, invoice, paid.Payment);
    }

    private PlatformInvoice Issue(PlatformTenant tenant, PlatformSubscription? subscription, string number, decimal price, DateTimeOffset at,
        bool withPeriod = true, PlatformInvoiceKind kind = PlatformInvoiceKind.FirstPeriod, string? term = null)
    {
        Dictionary<string, string> arguments = new(StringComparer.Ordinal);

        if (term is not null)
        {
            arguments["term"] = term;
        }

        PlatformInvoiceIssue issue = PlatformInvoice.Issue(
            tenant.Id, subscription?.Id, kind, number, "src-" + number, "USD", LanguageCode.En,
            PlatformInvoiceParty.Create("Galeo Logistics LLC", "1 Dock Road", null, "billing@galeo.test"),
            10, withPeriod ? at : null, withPeriod ? at.AddYears(1) : null,
            new[] { new PlatformInvoiceLineInput(kind == PlatformInvoiceKind.Charges ? PlatformInvoiceLineKind.OneTimeCharge : PlatformInvoiceLineKind.PlanPeriod, "ui.invoice_line.plan_period", arguments, 1m, price, 0m, null) },
            at);
        _kit.Billing.Invoices.Add(issue.Invoice);
        _kit.Billing.Events.Add(issue.Event);
        return issue.Invoice;
    }

    private PlatformSalesFeedRecorder Recorder()
    {
        return new PlatformSalesFeedRecorder(_feed, _kit.Billing, _kit.Kit.Store, _kit.Kit.Tenants.Tenants, _kit.Kit.Store, _kit.Payments, _kit.Kit.Sales);
    }

    private PlatformSalesFeedSender Sender()
    {
        return new PlatformSalesFeedSender(_feed, _feed, _kit.Kit.Tenants.Outbox(), _kit.Kit.Tenants.Store);
    }

    private static string Tenant(PlatformTenant tenant)
    {
        return "{\"id\":\"" + tenant.Id.ToString("D") + "\",\"code\":\"" + tenant.TenantCode + "\",\"legalName\":\"Northwind Freight Inc.\",\"displayName\":\"Northwind\","
            + "\"ownerEmail\":\"owner@northwind.example\",\"billingEmail\":\"owner@northwind.example\",\"country\":\"CA\",\"phone\":\"+15145550100\"}";
    }

    private static string Envelope(PlatformSalesFeedEvent item, string type, DateTimeOffset at, string tenant, string controlUrl, string data)
    {
        return "{\"id\":\"" + item.Id.ToString("D") + "\",\"type\":\"" + type + "\",\"occurredUtc\":\"" + at.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture)
            + "\",\"sequence\":" + item.Sequence.ToString(CultureInfo.InvariantCulture) + ",\"tenant\":" + tenant + ",\"controlUrl\":\"" + controlUrl + "\",\"data\":" + data + "}";
    }

    private static string Day(DateTimeOffset value) => value.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // =====================================================
    // Recording and the backfill
    // =====================================================
    [Fact]
    public async Task Backfill_WritesEveryHistoryRowOnce_InTimeOrder_WithItsOriginalTime()
    {
        History galeo = Galeo();

        (int written, bool more) = await Recorder().RecordAsync(Now, CancellationToken.None);
        (int again, _) = await Recorder().RecordAsync(Now, CancellationToken.None);

        Assert.Equal(4, written);
        Assert.False(more);
        Assert.Equal(0, again);
        Assert.Equal(
            new[] { PlatformSalesFeedTypes.SubscriptionChanged, PlatformSalesFeedTypes.InvoiceIssued, PlatformSalesFeedTypes.PaymentReceived, PlatformSalesFeedTypes.SubscriptionChanged },
            _feed.Events.OrderBy(item => item.Sequence).Select(item => item.EventType));
        Assert.Equal(new long[] { 1, 2, 3, 4 }, _feed.Events.Select(item => item.Sequence).OrderBy(item => item));
        Assert.Equal(new[] { Now.AddDays(-20), Now.AddDays(-19), Now.AddDays(-18), Now.AddDays(-17) }, _feed.Events.OrderBy(item => item.Sequence).Select(item => item.OccurredUtc));
        Assert.All(_feed.Events, item => Assert.Equal(PlatformSalesFeedStatus.Pending, item.Status));
        Assert.All(_feed.Events, item => Assert.Equal(galeo.Tenant.Id, item.PlatformTenantId));
    }

    [Fact]
    public async Task Recording_SkipsHistoryThatIsNotASale()
    {
        History galeo = Galeo();
        _kit.Billing.Events.Add(PlatformInvoiceEvent.Record(galeo.Invoice.Id, PlatformInvoiceEventCodes.Emailed, null, Now));

        (int written, _) = await Recorder().RecordAsync(Now, CancellationToken.None);

        Assert.Equal(4, written);
    }

    [Fact]
    public async Task InvoiceIssued_Json()
    {
        History galeo = Galeo();
        await Recorder().RecordAsync(Now, CancellationToken.None);
        PlatformSalesFeedEvent item = _feed.Events.Single(e => e.EventType == PlatformSalesFeedTypes.InvoiceIssued);

        string data = "{\"invoiceId\":\"" + galeo.Invoice.Id.ToString("D") + "\",\"number\":\"INV-2026-000001\",\"issuedOn\":\"" + Day(Now.AddDays(-19)) + "\",\"dueOn\":\"" + Day(galeo.Invoice.DueUtc)
            + "\",\"periodStart\":\"" + Day(Now.AddDays(-19)) + "\",\"periodEnd\":\"" + Day(Now.AddDays(-19).AddYears(1)) + "\",\"currency\":\"USD\",\"subtotal\":\"5400.00\",\"tax\":\"0.00\",\"total\":\"5400.00\",\"planCode\":\"PRO\",\"licences\":10}";
        Assert.Equal(Envelope(item, "invoice.issued", Now.AddDays(-19), Tenant(galeo.Tenant), "https://control.vixly.test/invoices/" + galeo.Invoice.Id.ToString("D"), data), item.Payload);
        Assert.Equal(5400m, item.Amount);
        Assert.Equal("USD", item.CurrencyCode);
    }

    // TechOS, 2026-11-03: a first-period invoice is issued before payment starts the period; it still carries the period it will cover.
    [Theory]
    [InlineData("Monthly", 1)]
    [InlineData("Annual", 12)]
    public async Task InvoiceIssued_FirstPeriodBeforePayment_CarriesThePeriodFromItsIssueDate(string term, int months)
    {
        PlatformTenant tenant = _kit.Kit.Tenants.Tenant("galeo", active: true);
        Issue(tenant, null, "INV-2026-000009", 100m, Now.AddDays(-3), withPeriod: false, term: term);

        await Recorder().RecordAsync(Now, CancellationToken.None);
        string payload = _feed.Events.Single(e => e.EventType == PlatformSalesFeedTypes.InvoiceIssued).Payload;

        Assert.Contains("\"periodStart\":\"" + Day(Now.AddDays(-3)) + "\",\"periodEnd\":\"" + Day(Now.AddDays(-3).AddMonths(months)) + "\"", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvoiceIssued_OneTimeCharges_HasNoPeriod()
    {
        PlatformTenant tenant = _kit.Kit.Tenants.Tenant("galeo", active: true);
        Issue(tenant, null, "INV-2026-000010", 40m, Now.AddDays(-3), withPeriod: false, kind: PlatformInvoiceKind.Charges);

        await Recorder().RecordAsync(Now, CancellationToken.None);

        Assert.Contains("\"periodStart\":null,\"periodEnd\":null", _feed.Events.Single(e => e.EventType == PlatformSalesFeedTypes.InvoiceIssued).Payload, StringComparison.Ordinal);
    }

    // TechOS, 2026-11-03: a rejected event is written again under a new id; the rejected one is kept as replaced and left out of the totals.
    [Fact]
    public async Task Rebuild_WritesACorrectedEvent_UnderANewIdAndSequence_AndReplacesTheRejectedOne()
    {
        History galeo = Galeo();
        await Recorder().RecordAsync(Now, CancellationToken.None);
        PlatformSalesFeedEvent rejected = _feed.Events.Single(e => e.EventType == PlatformSalesFeedTypes.InvoiceIssued);
        rejected.Park("data.periodStart is required.", Now);

        PlatformSalesFeedEvent corrected = Assert.IsType<PlatformSalesFeedEvent>(await Recorder().RebuildAsync(rejected, Now, CancellationToken.None));
        rejected.Replace(Now);
        _feed.Add(corrected);

        Assert.NotEqual(rejected.Id, corrected.Id);
        Assert.Equal(5, corrected.Sequence);
        Assert.Equal(PlatformSalesFeedSource.Correction, corrected.Source);
        Assert.Equal(rejected.Id, corrected.SourceId);
        Assert.Equal(rejected.OccurredUtc, corrected.OccurredUtc);
        Assert.Equal(PlatformSalesFeedStatus.Pending, corrected.Status);
        Assert.Equal(PlatformSalesFeedStatus.Replaced, rejected.Status);
        Assert.Contains("\"number\":\"INV-2026-000001\"", corrected.Payload, StringComparison.Ordinal);
        Assert.Contains("\"id\":\"" + corrected.Id.ToString("D") + "\"", corrected.Payload, StringComparison.Ordinal);
        Assert.Single(await _feed.ListDayAsync(Now.AddDays(-20), Now, CancellationToken.None), item => item.EventType == PlatformSalesFeedTypes.InvoiceIssued);

        // A correction that is rejected again is rebuilt from the same history row.
        corrected.Park("again", Now);
        PlatformSalesFeedEvent second = Assert.IsType<PlatformSalesFeedEvent>(await Recorder().RebuildAsync(corrected, Now, CancellationToken.None));
        Assert.Equal(corrected.Id, second.SourceId);
        Assert.Equal(0, (await Recorder().RecordAsync(Now, CancellationToken.None)).Written);
        Assert.Equal(galeo.Invoice.Id.ToString("D"), System.Text.Json.JsonDocument.Parse(second.Payload).RootElement.GetProperty("data").GetProperty("invoiceId").GetString());
    }

    [Fact]
    public void Replace_OnlyARejectedEvent()
    {
        PlatformSalesFeedEvent item = PlatformSalesFeedEvent.Create(1, PlatformSalesFeedTypes.InvoiceIssued, PlatformSalesFeedSource.InvoiceEvent, Guid.NewGuid(), null, null, Now, 1m, "USD", id => "{}", Now);

        Assert.Throws<Vixly.Shared.Kernel.Domain.DomainException>(() => item.Replace(Now));
    }

    [Fact]
    public async Task PaymentReceived_Json_ManualPaymentReadsManual()
    {
        History galeo = Galeo();
        await Recorder().RecordAsync(Now, CancellationToken.None);
        PlatformSalesFeedEvent item = _feed.Events.Single(e => e.EventType == PlatformSalesFeedTypes.PaymentReceived);

        string data = "{\"paymentId\":\"" + galeo.Payment.Id.ToString("D") + "\",\"invoiceNumber\":\"INV-2026-000001\",\"amount\":\"5400.00\",\"currency\":\"USD\",\"method\":\"manual\",\"receivedOn\":\"" + Day(Now.AddDays(-18)) + "\"}";
        Assert.Equal(Envelope(item, "payment.received", Now.AddDays(-18), Tenant(galeo.Tenant), "https://control.vixly.test/invoices/" + galeo.Invoice.Id.ToString("D"), data), item.Payload);
    }

    [Fact]
    public async Task PaymentReceived_ByCardThroughAWallet_ReadsWallet()
    {
        History galeo = Galeo(PlatformPaymentMethod.Card);
        PlatformCardTransaction charge = PlatformCardTransaction.Charge(galeo.Tenant.Id, galeo.Invoice.Id, PlatformCardTransactionOrigin.PayLink, PlatformCardProvider.Stripe, 5400m, "USD", null, null, Now);
        charge.Succeed("WIRE-1", Now);
        charge.RecordWallet("apple_pay", Now);
        _kit.Payments.Transactions.Add(charge);

        await Recorder().RecordAsync(Now, CancellationToken.None);

        Assert.Contains("\"method\":\"wallet\"", _feed.Events.Single(e => e.EventType == PlatformSalesFeedTypes.PaymentReceived).Payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PaymentReversed_Json_ReversalIdIsVixlysOwnRecord()
    {
        History galeo = Galeo();
        PlatformInvoiceEvent reversed = galeo.Invoice.ReversePayment(galeo.Payment.Id, "Bounced", Actor, Now.AddDays(-5));
        _kit.Billing.Events.Add(reversed);

        await Recorder().RecordAsync(Now, CancellationToken.None);
        PlatformSalesFeedEvent item = _feed.Events.Single(e => e.EventType == PlatformSalesFeedTypes.PaymentReversed);

        string data = "{\"paymentId\":\"" + galeo.Payment.Id.ToString("D") + "\",\"invoiceNumber\":\"INV-2026-000001\",\"amount\":\"5400.00\",\"currency\":\"USD\",\"reason\":\"refund\",\"reversalId\":\"" + reversed.Id.ToString("D") + "\"}";
        Assert.Equal(Envelope(item, "payment.reversed", Now.AddDays(-5), Tenant(galeo.Tenant), "https://control.vixly.test/invoices/" + galeo.Invoice.Id.ToString("D"), data), item.Payload);
        Assert.Equal(5400m, item.Amount);
    }

    [Fact]
    public async Task InvoiceVoided_And_InvoiceCredited_Json()
    {
        History galeo = Galeo();
        PlatformInvoice second = Issue(galeo.Tenant, galeo.Subscription, "INV-2026-000002", 100m, Now.AddDays(-3));
        _kit.Billing.Events.Add(second.RequestCredit(0m, true, "Wrong invoice", Actor, Now.AddDays(-2)));
        PlatformInvoiceCreditResult voided = second.DecideCredit(second.PendingCredit!.Id, true, "CN-2026-000001", null, Guid.NewGuid(), Now.AddDays(-2));
        _kit.Billing.Events.Add(voided.Event);
        _kit.Billing.Events.Add(galeo.Invoice.RequestCredit(400m, false, "Goodwill", Actor, Now.AddDays(-1)));
        PlatformInvoiceCreditResult credited = galeo.Invoice.DecideCredit(galeo.Invoice.PendingCredit!.Id, true, "CN-2026-000002", null, Guid.NewGuid(), Now.AddDays(-1));
        _kit.Billing.Events.Add(credited.Event);

        await Recorder().RecordAsync(Now, CancellationToken.None);
        PlatformSalesFeedEvent voidItem = _feed.Events.Single(e => e.EventType == PlatformSalesFeedTypes.InvoiceVoided);
        PlatformSalesFeedEvent creditItem = _feed.Events.Single(e => e.EventType == PlatformSalesFeedTypes.InvoiceCredited);

        Assert.Equal(
            Envelope(voidItem, "invoice.voided", Now.AddDays(-2), Tenant(galeo.Tenant), "https://control.vixly.test/invoices/" + second.Id.ToString("D"), "{\"invoiceNumber\":\"INV-2026-000002\",\"amount\":\"100.00\",\"currency\":\"USD\"}"),
            voidItem.Payload);
        Assert.Equal(
            Envelope(creditItem, "invoice.credited", Now.AddDays(-1), Tenant(galeo.Tenant), "https://control.vixly.test/invoices/" + galeo.Invoice.Id.ToString("D"), "{\"invoiceNumber\":\"INV-2026-000001\",\"amount\":\"400.00\",\"currency\":\"USD\",\"creditNoteNumber\":\"CN-2026-000002\"}"),
            creditItem.Payload);
    }

    [Fact]
    public async Task SubscriptionChanged_Json()
    {
        History galeo = Galeo();
        await Recorder().RecordAsync(Now, CancellationToken.None);
        PlatformSalesFeedEvent first = _feed.Events.OrderBy(e => e.Sequence).First();
        PlatformSalesFeedEvent activated = _feed.Events.OrderBy(e => e.Sequence).Last();
        string renews = Day(galeo.Subscription.PeriodEndUtc!.Value);

        Assert.Equal(
            Envelope(first, "subscription.changed", Now.AddDays(-20), Tenant(galeo.Tenant), "https://control.vixly.test/tenants/" + galeo.Tenant.Id.ToString("D"),
                "{\"subscriptionId\":\"" + galeo.Subscription.Id.ToString("D") + "\",\"change\":\"started\",\"planCode\":\"PRO\",\"planName\":\"PRO plan\",\"licences\":10,\"term\":\"annual\","
                + "\"amountPerPeriod\":\"5500.00\",\"currency\":\"USD\",\"status\":\"pending_payment\",\"renewsOn\":\"" + renews + "\"}"),
            first.Payload);
        Assert.Contains("\"status\":\"active\"", activated.Payload, StringComparison.Ordinal);
        Assert.Null(first.Amount);
    }

    [Fact]
    public async Task Events_NeverCarryCardOrBankDetails()
    {
        Galeo(PlatformPaymentMethod.Card);

        await Recorder().RecordAsync(Now, CancellationToken.None);

        Assert.All(_feed.Events, item =>
        {
            Assert.DoesNotContain("WIRE-1", item.Payload, StringComparison.Ordinal);
            Assert.DoesNotContain("4242", item.Payload, StringComparison.Ordinal);
            Assert.DoesNotContain("pm_", item.Payload, StringComparison.Ordinal);
        });
    }

    // =====================================================
    // Mapping
    // =====================================================
    [Theory]
    [InlineData(PlatformSubscriptionStatus.Trialing, "trial")]
    [InlineData(PlatformSubscriptionStatus.PendingPayment, "pending_payment")]
    [InlineData(PlatformSubscriptionStatus.Active, "active")]
    [InlineData(PlatformSubscriptionStatus.PastDue, "past_due")]
    [InlineData(PlatformSubscriptionStatus.Suspended, "suspended")]
    [InlineData(PlatformSubscriptionStatus.BlockedForNonPayment, "blocked")]
    [InlineData(PlatformSubscriptionStatus.Ended, "ended")]
    public void Status_MapsOneToOne(PlatformSubscriptionStatus status, string expected)
    {
        Assert.Equal(expected, PlatformSalesFeedMapping.Status(status));
    }

    [Fact]
    public void Status_CoversEveryValue()
    {
        Assert.All(Enum.GetValues<PlatformSubscriptionStatus>(), status => Assert.False(string.IsNullOrEmpty(PlatformSalesFeedMapping.Status(status))));
    }

    [Theory]
    [InlineData(PlatformPaymentMethod.Card, null, "card")]
    [InlineData(PlatformPaymentMethod.Card, "google_pay", "wallet")]
    [InlineData(PlatformPaymentMethod.BankDebit, null, "us_bank")]
    [InlineData(PlatformPaymentMethod.Zelle, null, "zelle")]
    [InlineData(PlatformPaymentMethod.BankTransfer, null, "manual")]
    [InlineData(PlatformPaymentMethod.Cash, null, "manual")]
    [InlineData(PlatformPaymentMethod.Cheque, null, "manual")]
    [InlineData(PlatformPaymentMethod.Other, null, "manual")]
    public void Method_Maps(PlatformPaymentMethod method, string? wallet, string expected)
    {
        Assert.Equal(expected, PlatformSalesFeedMapping.Method(method, wallet));
    }

    [Theory]
    [InlineData(PlatformInvoiceEventCodes.CardRefunded, PlatformPaymentMethod.Card, "refund")]
    [InlineData(PlatformInvoiceEventCodes.DisputeLost, PlatformPaymentMethod.Card, "chargeback")]
    [InlineData(PlatformInvoiceEventCodes.PaymentReversed, PlatformPaymentMethod.BankDebit, "bank_failed")]
    [InlineData(PlatformInvoiceEventCodes.PaymentReversed, PlatformPaymentMethod.BankTransfer, "refund")]
    public void Reason_Maps(string code, PlatformPaymentMethod method, string expected)
    {
        Assert.Equal(expected, PlatformSalesFeedMapping.Reason(code, method));
    }

    [Fact]
    public void Change_TellsLicencesFromPlan()
    {
        Dictionary<string, string> licences = new(StringComparer.Ordinal) { ["plan"] = "PRO", ["fromPlan"] = "PRO", ["term"] = "Annual", ["fromTerm"] = "Annual", ["licences"] = "12", ["fromLicences"] = "10" };
        Dictionary<string, string> plan = new(licences, StringComparer.Ordinal) { ["fromPlan"] = "GROWTH" };

        Assert.Equal("licences_changed", PlatformSalesFeedMapping.Change(PlatformSubscriptionEventCodes.Changed, licences));
        Assert.Equal("plan_changed", PlatformSalesFeedMapping.Change(PlatformSubscriptionEventCodes.Changed, plan));
        Assert.Equal("renewed", PlatformSalesFeedMapping.Change(PlatformSubscriptionEventCodes.Renewed, plan));
        Assert.Equal("blocked", PlatformSalesFeedMapping.Change(PlatformSubscriptionEventCodes.BlockedForNonPayment, plan));
        Assert.Equal("reactivated", PlatformSalesFeedMapping.Change(PlatformSubscriptionEventCodes.BlockLifted, plan));
        Assert.Equal("ended", PlatformSalesFeedMapping.Change(PlatformSubscriptionEventCodes.Ended, plan));
        Assert.Null(PlatformSalesFeedMapping.Change(PlatformSubscriptionEventCodes.DiscountApplied, plan));
        Assert.All(PlatformSalesFeedMapping.SubscriptionCodes, code => Assert.NotNull(PlatformSalesFeedMapping.Change(code, plan)));
    }

    [Theory]
    [InlineData("5400", "5400.00")]
    [InlineData("0", "0.00")]
    [InlineData("12.345", "12.35")]
    [InlineData("-7.5", "7.50")]
    public void Money_HasExactlyTwoDecimals_NeverNegative(string amount, string expected)
    {
        Assert.Equal(expected, PlatformSalesFeedMapping.Money(decimal.Parse(amount, CultureInfo.InvariantCulture)));
    }

    // =====================================================
    // Daily totals
    // =====================================================
    [Fact]
    public async Task Totals_OnePerClosedDay_TypesWithMoneyAndEmptySums_WrittenOnce()
    {
        History galeo = Galeo();
        await Recorder().RecordAsync(Now, CancellationToken.None);

        int written = await Recorder().TotalsAsync(Now, CancellationToken.None);
        int again = await Recorder().TotalsAsync(Now, CancellationToken.None);

        Assert.Equal(20, written);
        Assert.Equal(0, again);
        PlatformSalesFeedEvent day = _feed.Events.Single(item => item.TotalsDate == DateOnly.FromDateTime(Now.AddDays(-19).UtcDateTime));
        Assert.Equal(
            "{\"id\":\"" + day.Id.ToString("D") + "\",\"type\":\"totals.daily\",\"occurredUtc\":\"" + Day(Now.AddDays(-18)) + "T00:00:00.000Z\",\"sequence\":" + day.Sequence.ToString(CultureInfo.InvariantCulture)
            + ",\"tenant\":null,\"controlUrl\":\"https://control.vixly.test/invoices\",\"data\":{\"date\":\"" + Day(Now.AddDays(-19)) + "\",\"types\":{\"invoice.issued\":{\"count\":1,\"sums\":{\"USD\":\"5400.00\"}}}}}",
            day.Payload);
        PlatformSalesFeedEvent started = _feed.Events.Single(item => item.TotalsDate == DateOnly.FromDateTime(Now.AddDays(-20).UtcDateTime));
        Assert.Contains("\"subscription.changed\":{\"count\":1,\"sums\":{}}", started.Payload, StringComparison.Ordinal);
        PlatformSalesFeedEvent quiet = _feed.Events.Single(item => item.TotalsDate == DateOnly.FromDateTime(Now.AddDays(-10).UtcDateTime));
        Assert.Contains("\"types\":{}", quiet.Payload, StringComparison.Ordinal);
        Assert.Null(_feed.Events.SingleOrDefault(item => item.TotalsDate == DateOnly.FromDateTime(Now.UtcDateTime)));
        Assert.Equal(galeo.Tenant.Id, _feed.Events.First(item => item.Sequence == 1).PlatformTenantId);
    }

    [Fact]
    public async Task Totals_WaitUntilTheDayHasClosed()
    {
        Galeo();
        await Recorder().RecordAsync(Now, CancellationToken.None);
        DateTimeOffset midnight = new(Now.UtcDateTime.Date, TimeSpan.Zero);

        await Recorder().TotalsAsync(midnight.AddMinutes(5), CancellationToken.None);
        Assert.Null(_feed.Events.SingleOrDefault(item => item.TotalsDate == DateOnly.FromDateTime(midnight.AddDays(-1).UtcDateTime)));

        await Recorder().TotalsAsync(midnight.AddMinutes(11), CancellationToken.None);
        Assert.NotNull(_feed.Events.SingleOrDefault(item => item.TotalsDate == DateOnly.FromDateTime(midnight.AddDays(-1).UtcDateTime)));
    }

    // =====================================================
    // Sending
    // =====================================================
    [Fact]
    public async Task Send_AllAccepted_MarksThem_AndOldestFirstInOneBody()
    {
        Galeo();
        await Recorder().RecordAsync(Now, CancellationToken.None);
        _feed.Answers.Enqueue(FakeSalesFeedStore.AcceptAll);

        int answered = await Sender().SendAsync(() => Now, CancellationToken.None);

        Assert.Equal(4, answered);
        Assert.All(_feed.Events, item => Assert.Equal(PlatformSalesFeedStatus.Accepted, item.Status));
        Assert.Single(_feed.Bodies);
        Assert.StartsWith("{\"events\":[" + _feed.Events.OrderBy(item => item.Sequence).First().Payload + ",", _feed.Bodies[0], StringComparison.Ordinal);
        Assert.Equal(Now, _feed.State!.LastSuccessUtc);
    }

    [Fact]
    public async Task Send_Duplicates_AreDone_RejectedAreParkedAndAlerted_OthersGoOn()
    {
        Galeo();
        await Recorder().RecordAsync(Now, CancellationToken.None);
        PlatformSalesFeedEvent[] events = _feed.Events.OrderBy(item => item.Sequence).ToArray();
        _feed.Answers.Enqueue(_ => new PlatformSalesFeedAnswer(
            PlatformSalesFeedAnswerKind.Read, "http_200",
            new[] { events[0].Id, events[3].Id },
            new[] { events[1].Id },
            new[] { new PlatformSalesFeedRejection(events[2].Id, "amount missing") }));

        await Sender().SendAsync(() => Now, CancellationToken.None);

        Assert.Equal(PlatformSalesFeedStatus.Accepted, events[0].Status);
        Assert.Equal(PlatformSalesFeedStatus.Duplicate, events[1].Status);
        Assert.Equal(PlatformSalesFeedStatus.Parked, events[2].Status);
        Assert.Equal("amount missing", events[2].LastError);
        Assert.Equal(PlatformSalesFeedStatus.Accepted, events[3].Status);
        PlatformOutboxMessage alert = Assert.Single(_kit.Kit.Tenants.Tenants.Outbox, item => item.Type == PlatformMessageTypes.SalesFeedAlert);
        Assert.Equal(PlatformSalesFeedAlertKind.Parked, PlatformOutboxWriter.Read<PlatformSalesFeedAlert>(alert).Kind);
    }

    [Fact]
    public async Task Send_ParkedEventNeverBlocksTheNextOnes()
    {
        Galeo();
        await Recorder().RecordAsync(Now, CancellationToken.None);
        PlatformSalesFeedEvent first = _feed.Events.OrderBy(item => item.Sequence).First();
        first.Park("bad", Now);
        _feed.Answers.Enqueue(FakeSalesFeedStore.AcceptAll);

        await Sender().SendAsync(() => Now, CancellationToken.None);

        Assert.DoesNotContain(first.Id.ToString("D"), _feed.Bodies[0], StringComparison.Ordinal);
        Assert.Equal(3, _feed.Events.Count(item => item.Status == PlatformSalesFeedStatus.Accepted));
    }

    [Fact]
    public async Task Send_TooLarge_HalvesTheBatchAndTriesAgain()
    {
        Galeo();
        await Recorder().RecordAsync(Now, CancellationToken.None);
        _feed.Answers.Enqueue(_ => PlatformSalesFeedAnswer.Of(PlatformSalesFeedAnswerKind.TooLarge, "http_413"));
        _feed.Answers.Enqueue(FakeSalesFeedStore.AcceptAll);
        _feed.Answers.Enqueue(FakeSalesFeedStore.AcceptAll);

        await Sender().SendAsync(() => Now, CancellationToken.None);

        Assert.Equal(new[] { 4, 2, 2 }, _feed.Bodies.Select(body => FakeSalesFeedStore.Ids(body).Count));
        Assert.All(_feed.Events, item => Assert.Equal(PlatformSalesFeedStatus.Accepted, item.Status));
    }

    [Theory]
    [InlineData("http_400")]
    [InlineData("http_401")]
    public async Task Send_Refused_StopsKeepsEverythingAndAlerts_UntilStaffTryAgain(string code)
    {
        Galeo();
        await Recorder().RecordAsync(Now, CancellationToken.None);
        _feed.Answers.Enqueue(_ => PlatformSalesFeedAnswer.Of(PlatformSalesFeedAnswerKind.Refused, code));

        await Sender().SendAsync(() => Now, CancellationToken.None);
        await Sender().SendAsync(() => Now.AddHours(1), CancellationToken.None);

        Assert.Single(_feed.Bodies);
        Assert.All(_feed.Events, item => Assert.Equal(PlatformSalesFeedStatus.Pending, item.Status));
        Assert.Equal(code, _feed.State!.Problem);
        PlatformOutboxMessage alert = Assert.Single(_kit.Kit.Tenants.Tenants.Outbox, item => item.Type == PlatformMessageTypes.SalesFeedAlert);
        Assert.Equal(PlatformSalesFeedAlertKind.Stopped, PlatformOutboxWriter.Read<PlatformSalesFeedAlert>(alert).Kind);

        _feed.State.TryAgain(Now.AddHours(1));
        _feed.Answers.Enqueue(FakeSalesFeedStore.AcceptAll);
        await Sender().SendAsync(() => Now.AddHours(1), CancellationToken.None);
        Assert.All(_feed.Events, item => Assert.Equal(PlatformSalesFeedStatus.Accepted, item.Status));
    }

    [Theory]
    [InlineData("http_404")]
    [InlineData("http_503")]
    [InlineData("no_answer")]
    public async Task Send_NoAnswer_RetriesWithBackoff_AlertsOnceAfter24Hours_NeverParks(string code)
    {
        Galeo();
        await Recorder().RecordAsync(Now, CancellationToken.None);
        DateTimeOffset at = Now;

        for (int attempt = 0; attempt < 40; attempt++)
        {
            _feed.Answers.Enqueue(_ => PlatformSalesFeedAnswer.Of(PlatformSalesFeedAnswerKind.Retry, code));
            await Sender().SendAsync(() => at, CancellationToken.None);
            at = _feed.State!.NextAttemptUtc!.Value;
        }

        Assert.All(_feed.Events, item => Assert.Equal(PlatformSalesFeedStatus.Pending, item.Status));
        Assert.Equal(40, _feed.Bodies.Count);
        PlatformOutboxMessage alert = Assert.Single(_kit.Kit.Tenants.Tenants.Outbox, item => item.Type == PlatformMessageTypes.SalesFeedAlert);
        Assert.Equal(PlatformSalesFeedAlertKind.NoAnswer, PlatformOutboxWriter.Read<PlatformSalesFeedAlert>(alert).Kind);

        // Not due yet: nothing is sent.
        await Sender().SendAsync(() => at.AddSeconds(-1), CancellationToken.None);
        Assert.Equal(40, _feed.Bodies.Count);
    }

    [Fact]
    public async Task Send_Off_WhenNotConfigured_EventsStillWritten()
    {
        Galeo();
        _feed.Configured = false;

        await new PlatformSalesFeedClock(Recorder(), Sender(), _kit.Kit.Tenants.Store, _kit.Kit.Tenants.Clock).RunAsync(CancellationToken.None);

        Assert.Empty(_feed.Bodies);
        Assert.Equal(4, _feed.Events.Count(item => item.TotalsDate is null));
        Assert.All(_feed.Events, item => Assert.Equal(PlatformSalesFeedStatus.Pending, item.Status));
    }

    [Fact]
    public async Task SendAgain_PutsAParkedEventBackUnchanged()
    {
        Galeo();
        await Recorder().RecordAsync(Now, CancellationToken.None);
        PlatformSalesFeedEvent item = _feed.Events.First();
        string payload = item.Payload;
        item.Park("bad", Now);

        item.SendAgain(Now);

        Assert.Equal(PlatformSalesFeedStatus.Pending, item.Status);
        Assert.Equal(payload, item.Payload);
        Assert.Throws<Vixly.Shared.Kernel.Domain.DomainException>(() => item.SendAgain(Now));
    }

    [Fact]
    public async Task Alert_EmailsThePaymentsSettingsHolders()
    {
        PlatformOutboxMessage message = PlatformOutboxMessage.Create(PlatformMessageTypes.SalesFeedAlert, Guid.NewGuid(),
            JsonSerializer.Serialize(new PlatformSalesFeedAlert(PlatformSalesFeedAlertKind.Stopped, 3, "http_401", Now), new JsonSerializerOptions(JsonSerializerDefaults.Web)), Now);
        _kit.Kit.Tenants.Staff(Vixly.Platform.Application.Security.PlatformPermissions.PaymentsConfigure);

        await new PlatformSalesFeedAlertHandler(_kit.Kit.Tenants.Store, _feed, _kit.Kit.Tenants.Clock).HandleAsync(message, CancellationToken.None);

        (string _, PlatformSalesFeedAlert alert) = Assert.Single(_feed.Alerts);
        Assert.Equal(3, alert.Count);
        Assert.Equal("http_401", alert.Code);
    }
}
