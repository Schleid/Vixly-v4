using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformBillingTests.cs
//
// PURPOSE:
// Proves invoices, payments, credit notes, numbering, tax rates and billing settings (Slice 4).
//
// RESPONSIBILITIES:
// - Totals, discount and tax to the cent; nothing billed when the total is zero
// - Partial and full payment, refused overpayment, reversal
// - Credit notes and void with four eyes; past due once
// - Gap-free numbers, tax rate choice, settings validation, subscription hooks
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformBillingTests
{
    private static readonly DateTimeOffset Now = SubscriptionTestData.Now;

    private static readonly Guid Clerk = Guid.Parse("0199e3a0-0000-7000-8000-000000000011");

    private static readonly Guid Manager = Guid.Parse("0199e3a0-0000-7000-8000-000000000012");

    private static PlatformInvoiceLineInput Line(PlatformInvoiceLineKind kind, decimal quantity, decimal price, decimal rate = 0m)
    {
        return new PlatformInvoiceLineInput(kind, "ui.invoice_line.test", new Dictionary<string, string>(StringComparer.Ordinal), quantity, price, rate, rate > 0m ? "VAT" : null);
    }

    private static PlatformInvoice Invoice(int terms = 7, params PlatformInvoiceLineInput[] lines)
    {
        PlatformInvoiceLineInput[] chosen = lines.Length > 0 ? lines : new[] { Line(PlatformInvoiceLineKind.PlanPeriod, 3m, 25m, 20m) };

        return PlatformInvoice.Issue(
            SubscriptionTestData.Tenant,
            Guid.NewGuid(),
            PlatformInvoiceKind.Renewal,
            "INV-2026-000001",
            "source-" + Guid.NewGuid().ToString("N"),
            "usd",
            LanguageCode.Default,
            PlatformInvoiceParty.Create("Atlas Freight LLC", "1 Dock Road\nMiami FL\nUS", "US-123", "billing@atlas.test"),
            terms,
            Now,
            Now.AddMonths(1),
            chosen,
            Now).Invoice;
    }

    [Fact]
    public void Issue_ComputesNetDiscountTaxAndTotal_ToTheCent()
    {
        PlatformInvoice invoice = Invoice(7,
            Line(PlatformInvoiceLineKind.PlanPeriod, 3m, 25m, 20m),
            Line(PlatformInvoiceLineKind.SetupFee, 1m, 99.99m, 20m),
            Line(PlatformInvoiceLineKind.Discount, 1m, -7.50m, 20m));

        Assert.Equal(174.99m, invoice.NetTotal);
        Assert.Equal(7.50m, invoice.DiscountTotal);
        Assert.Equal(15m + 20m - 1.5m, invoice.TaxTotal);
        Assert.Equal(174.99m - 7.50m + 33.50m, invoice.Total);
        Assert.Equal(invoice.Total, invoice.Balance);
        Assert.Equal("USD", invoice.CurrencyCode);
        Assert.Equal(Now.AddDays(7), invoice.DueUtc);
        Assert.Equal(PlatformInvoiceStatus.Issued, invoice.Status);
    }

    [Fact]
    public void Issue_RoundsTaxHalfAwayFromZero()
    {
        PlatformInvoice invoice = Invoice(0, Line(PlatformInvoiceLineKind.OneTimeCharge, 1m, 0.05m, 10m));

        Assert.Equal(0.01m, invoice.TaxTotal);
        Assert.Equal(Now, invoice.DueUtc);
    }

    [Fact]
    public void Issue_RefusesZeroTotal_AndBadLines()
    {
        SubscriptionTestData.AssertCode("platform.billing.nothing_to_bill", () => Invoice(7, Line(PlatformInvoiceLineKind.PlanPeriod, 1m, 10m), Line(PlatformInvoiceLineKind.Discount, 1m, -10m)));
        SubscriptionTestData.AssertCode("platform.billing.line_invalid", () => Invoice(7, Line(PlatformInvoiceLineKind.PlanPeriod, 1m, -10m)));
        SubscriptionTestData.AssertCode("platform.billing.line_invalid", () => Invoice(7, Line(PlatformInvoiceLineKind.Discount, 1m, 5m)));
        SubscriptionTestData.AssertCode("platform.billing.line_invalid", () => Invoice(7, Line(PlatformInvoiceLineKind.PlanPeriod, 1m, 10.001m)));
    }

    [Fact]
    public void Payments_PartialThenFull_SettleTheInvoice()
    {
        PlatformInvoice invoice = Invoice(7, Line(PlatformInvoiceLineKind.PlanPeriod, 1m, 100m));

        PlatformInvoicePaymentResult first = invoice.RecordPayment("RCT-2026-000001", 40m, PlatformPaymentMethod.BankTransfer, Now, "WIRE-1", null, Clerk, Now);
        Assert.False(first.BecamePaid);
        Assert.True(invoice.IsPartlyPaid);
        Assert.Equal(60m, invoice.Balance);

        PlatformInvoicePaymentResult second = invoice.RecordPayment("RCT-2026-000002", 60m, PlatformPaymentMethod.Cash, Now, null, null, Clerk, Now);
        Assert.True(second.BecamePaid);
        Assert.Equal(PlatformInvoiceStatus.Paid, invoice.Status);
        Assert.Equal(0m, invoice.Balance);
        Assert.Equal(PlatformInvoiceEventCodes.PaymentRecorded, second.Event.EventCode);
    }

    [Fact]
    public void Payments_RefuseOverpayment_MissingReference_FutureDate_AndPaidInvoice()
    {
        PlatformInvoice invoice = Invoice(7, Line(PlatformInvoiceLineKind.PlanPeriod, 1m, 100m));

        SubscriptionTestData.AssertCode("platform.billing.amount_above_balance", () => invoice.RecordPayment("R1", 100.01m, PlatformPaymentMethod.Cash, Now, null, null, Clerk, Now));
        SubscriptionTestData.AssertCode("platform.billing.reference_required", () => invoice.RecordPayment("R1", 10m, PlatformPaymentMethod.Cheque, Now, " ", null, Clerk, Now));
        SubscriptionTestData.AssertCode("platform.billing.received_date_invalid", () => invoice.RecordPayment("R1", 10m, PlatformPaymentMethod.Cash, Now.AddDays(1), null, null, Clerk, Now));
        SubscriptionTestData.AssertCode("platform.billing.amount_invalid", () => invoice.RecordPayment("R1", 0m, PlatformPaymentMethod.Cash, Now, null, null, Clerk, Now));

        invoice.RecordPayment("R1", 100m, PlatformPaymentMethod.Cash, Now, null, null, Clerk, Now);
        SubscriptionTestData.AssertCode("platform.billing.invoice_paid", () => invoice.RecordPayment("R2", 1m, PlatformPaymentMethod.Cash, Now, null, null, Clerk, Now));
    }

    [Fact]
    public void Reversal_ReopensTheBalance_Once()
    {
        PlatformInvoice invoice = Invoice(7, Line(PlatformInvoiceLineKind.PlanPeriod, 1m, 100m));
        PlatformPayment payment = invoice.RecordPayment("R1", 100m, PlatformPaymentMethod.Cheque, Now, "CHQ 551", null, Clerk, Now).Payment;

        PlatformInvoiceEvent reversed = invoice.ReversePayment(payment.Id, "Cheque bounced", Clerk, Now);

        Assert.Equal(PlatformInvoiceEventCodes.PaymentReversed, reversed.EventCode);
        Assert.Equal(PlatformInvoiceStatus.Issued, invoice.Status);
        Assert.Equal(100m, invoice.Balance);
        SubscriptionTestData.AssertCode("platform.billing.payment_reversed", () => invoice.ReversePayment(payment.Id, "Again", Clerk, Now));
        SubscriptionTestData.AssertCode("platform.billing.reason_required", () => invoice.ReversePayment(payment.Id, " ", Clerk, Now));
    }

    [Fact]
    public void Credit_NeedsASecondPerson_AndLowersTheBalance()
    {
        PlatformInvoice invoice = Invoice(7, Line(PlatformInvoiceLineKind.PlanPeriod, 1m, 100m, 10m));

        invoice.RequestCredit(30m, false, "Outage on 3 days", Clerk, Now);
        SubscriptionTestData.AssertCode("platform.billing.credit_pending", () => invoice.RequestCredit(5m, false, "Again", Clerk, Now));
        Guid creditId = invoice.PendingCredit!.Id;

        SubscriptionTestData.AssertCode("platform.billing.own_request", () => invoice.DecideCredit(creditId, true, "CN-2026-000001", null, Clerk, Now));
        PlatformInvoiceCreditResult issued = invoice.DecideCredit(creditId, true, "CN-2026-000001", null, Manager, Now);

        Assert.Equal(PlatformInvoiceEventCodes.CreditIssued, issued.Event.EventCode);
        Assert.Equal(2.73m, issued.Credit.TaxPart);
        Assert.Equal(80m, invoice.Balance);
        Assert.Equal("CN-2026-000001", issued.Credit.Number);
    }

    [Fact]
    public void Credit_AfterFullPayment_IsMoneyOwedBack()
    {
        PlatformInvoice invoice = Invoice(7, Line(PlatformInvoiceLineKind.PlanPeriod, 1m, 100m));
        invoice.RecordPayment("R1", 100m, PlatformPaymentMethod.Cash, Now, null, null, Clerk, Now);
        invoice.RequestCredit(25m, false, "Goodwill", Clerk, Now);

        invoice.DecideCredit(invoice.PendingCredit!.Id, true, "CN-1", null, Manager, Now);

        Assert.Equal(25m, invoice.RefundOwed);
        Assert.Equal(PlatformInvoiceStatus.Paid, invoice.Status);
        SubscriptionTestData.AssertCode("platform.billing.credit_above_remaining", () => invoice.RequestCredit(75.01m, false, "Too much", Clerk, Now));
    }

    [Fact]
    public void Void_OnlyBeforePayment_AndRejectNeedsAReason()
    {
        PlatformInvoice paid = Invoice(7, Line(PlatformInvoiceLineKind.PlanPeriod, 1m, 100m));
        paid.RecordPayment("R1", 10m, PlatformPaymentMethod.Cash, Now, null, null, Clerk, Now);
        SubscriptionTestData.AssertCode("platform.billing.void_after_payment", () => paid.RequestCredit(0m, true, "Wrong plan", Clerk, Now));

        PlatformInvoice open = Invoice(7, Line(PlatformInvoiceLineKind.PlanPeriod, 1m, 100m));
        open.RequestCredit(0m, true, "Wrong plan", Clerk, Now);
        SubscriptionTestData.AssertCode("platform.billing.reason_required", () => open.DecideCredit(open.PendingCredit!.Id, false, null, null, Manager, Now));

        PlatformInvoiceCreditResult voided = open.DecideCredit(open.PendingCredit!.Id, true, "CN-2", null, Manager, Now);

        Assert.Equal(PlatformInvoiceEventCodes.Voided, voided.Event.EventCode);
        Assert.Equal(PlatformInvoiceStatus.Void, open.Status);
        Assert.Equal(100m, voided.Credit.Amount);
        SubscriptionTestData.AssertCode("platform.billing.invoice_void", () => open.RecordPayment("R2", 1m, PlatformPaymentMethod.Cash, Now, null, null, Clerk, Now));
    }

    [Fact]
    public void PastDue_IsMarkedOnce_AfterTheDueDate()
    {
        PlatformInvoice invoice = Invoice(7, Line(PlatformInvoiceLineKind.PlanPeriod, 1m, 100m));

        Assert.Null(invoice.MarkPastDueIfOverdue(Now.AddDays(7)));
        Assert.NotNull(invoice.MarkPastDueIfOverdue(Now.AddDays(8)));
        Assert.Null(invoice.MarkPastDueIfOverdue(Now.AddDays(9)));
    }

    [Fact]
    public void DueOnIssue_IsNotOverdueOnTheSameDay_OnlyFromTheNextDay()
    {
        PlatformInvoice invoice = Invoice(0, Line(PlatformInvoiceLineKind.PlanPeriod, 1m, 100m));
        DateTimeOffset endOfDay = new(Now.UtcDateTime.Date.AddDays(1).AddTicks(-1), TimeSpan.Zero);

        Assert.False(invoice.IsOverdueAt(endOfDay));
        Assert.True(invoice.IsOverdueAt(endOfDay.AddTicks(1)));
    }

    [Fact]
    public void Sequence_HandsOutConsecutiveNumbers()
    {
        PlatformNumberSequence sequence = PlatformNumberSequence.Start(PlatformBillingDocumentKind.Invoice, 2026, Now);

        Assert.Equal("INV-2026-000001", sequence.Take("INV", Now));
        Assert.Equal("INV-2026-000002", sequence.Take("INV", Now));
    }

    [Fact]
    public void TaxRate_PrefersTheRegion_AndTheNewestRateInForce()
    {
        PlatformTaxRate country = PlatformTaxRate.Create("ca", null, 5m, "GST", Now.AddYears(-1), Clerk, Now);
        PlatformTaxRate quebec = PlatformTaxRate.Create("CA", "qc", 14.975m, "GST+QST", Now.AddYears(-1), Clerk, Now);
        PlatformTaxRate future = PlatformTaxRate.Create("CA", null, 6m, "GST", Now.AddDays(10), Clerk, Now);
        PlatformTaxRate[] rates = { country, quebec, future };

        Assert.Same(quebec, PlatformTaxRate.Pick(rates, "CA", "QC", Now));
        Assert.Same(country, PlatformTaxRate.Pick(rates, "CA", "ON", Now));
        Assert.Same(future, PlatformTaxRate.Pick(rates, "CA", null, Now.AddDays(11)));
        Assert.Null(PlatformTaxRate.Pick(rates, "US", null, Now));
        SubscriptionTestData.AssertCode("platform.billing.tax_rate_invalid", () => PlatformTaxRate.Create("CA", null, 100.5m, "X", Now, Clerk, Now));
        SubscriptionTestData.AssertCode("platform.billing.country_invalid", () => PlatformTaxRate.Create("ZZ", null, 5m, "X", Now, Clerk, Now));
    }

    [Fact]
    public void Settings_DueOnIssueUntilTermsAreSet_AndPrefixesMustDiffer()
    {
        PlatformBillingSettings settings = PlatformBillingSettings.Create(Now);
        Assert.Equal(0, settings.DueDays);

        Dictionary<LanguageCode, (string?, string?)> texts = new() { [LanguageCode.Default] = ("Pay by wire to ...", null) };
        settings.Update(30, "inv", "cn", "rct", PlatformPaperSize.Letter, false, texts, Clerk, Now);

        Assert.Equal(30, settings.DueDays);
        Assert.Equal("INV", settings.PrefixFor(PlatformBillingDocumentKind.Invoice));
        Assert.Equal("Pay by wire to ...", settings.TextIn(LanguageCode.Create("fr"))!.PaymentInstructions);
        SubscriptionTestData.AssertCode("platform.billing.prefix_invalid", () => settings.Update(30, "INV", "INV", "RCT", PlatformPaperSize.A4, true, texts, Clerk, Now));
        SubscriptionTestData.AssertCode("platform.billing.prefix_invalid", () => settings.Update(30, "IN-V", "CN", "RCT", PlatformPaperSize.A4, true, texts, Clerk, Now));
        SubscriptionTestData.AssertCode("platform.billing.setting_invalid", () => settings.Update(400, "INV", "CN", "RCT", PlatformPaperSize.A4, true, texts, Clerk, Now));
    }

    [Fact]
    public void Subscription_InvoicePaid_ActivatesOrClearsPastDue()
    {
        PlatformPlan plan = SubscriptionTestData.Published();
        PlatformSubscription waiting = SubscriptionTestData.Started(plan);

        PlatformSubscriptionEvent? activated = waiting.InvoicePaid("INV-2026-000001", false, Now);
        Assert.Equal(PlatformSubscriptionEventCodes.InvoicePaid, activated!.EventCode);
        Assert.Equal(PlatformSubscriptionStatus.Active, waiting.Status);
        Assert.Equal("INV-2026-000001", activated.Arguments["invoice"]);

        PlatformSubscriptionEvent? pastDue = waiting.MarkPastDue("INV-2026-000002", Now);
        Assert.Equal(PlatformSubscriptionStatus.PastDue, waiting.Status);
        Assert.NotNull(pastDue);
        Assert.Null(waiting.InvoicePaid("INV-2026-000002", true, Now));
        Assert.Equal(PlatformSubscriptionStatus.PastDue, waiting.Status);
        Assert.NotNull(waiting.InvoicePaid("INV-2026-000003", false, Now));
        Assert.Equal(PlatformSubscriptionStatus.Active, waiting.Status);
    }

    [Fact]
    public void Subscription_MarksChargesBilled()
    {
        PlatformPlan plan = SubscriptionTestData.Published();
        PlatformSubscription subscription = SubscriptionTestData.Active(plan);
        subscription.AddCharge("Training", 250m, "USD", "Asked by customer", SubscriptionTestData.Actor, Now);

        subscription.MarkChargesBilled(subscription.PendingCharges.Select(item => item.Id).ToArray(), Now);

        Assert.Empty(subscription.PendingCharges);
        Assert.Equal(PlatformOneTimeChargeStatus.Billed, subscription.Charges.Single().Status);
    }
}
