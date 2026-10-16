using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Application.UseCases.Billing.DecidePlatformCreditNote;
using Vixly.Platform.Application.UseCases.Billing.RecordPlatformPayment;
using Vixly.Platform.Application.UseCases.Billing.RequestPlatformCreditNote;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformBillingUseCaseTests.cs
//
// PURPOSE:
// Proves Slice 4 billing from subscription messages, payments, credit notes and the past-due clock.
//
// RESPONSIBILITIES:
// - First-period invoice with plan, setup fee, discount, charges and tax; no double billing
// - Paying the waiting invoice activates the subscription; receipts are stored
// - Credit notes need a second person; the clock marks renewals past due
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformBillingUseCaseTests
{
    private readonly SubscriptionTestKit _kit = new();

    private readonly FakeBillingStore _billing = new();

    private PlatformBillingAdministration Billing()
    {
        return new PlatformBillingAdministration(
            _kit.Tenants.Gate(),
            new PlatformStepUpGuard(_kit.Tenants.Store, _kit.Tenants.Clock),
            _kit.Tenants.Store,
            _billing,
            _billing,
            _billing,
            _kit.Tenants.Audit(),
            _kit.Tenants.Outbox(),
            _kit.Tenants.Clock);
    }

    private PlatformCompanyProfile Profile()
    {
        return PlatformCompanyProfile.Create(
            PlatformCompanyDetails.Create("TechCrafters Inc.", "Vixly", EmailAddress.Create("billing@vixly.example"), PhoneNumber.Create("+1 305 555 0100"),
                PlatformPostalAddress.Create("100 Biscayne Blvd", null, "Miami", "FL", "33132", "US")),
            null, null, Guid.NewGuid(), _kit.Now);
    }

    private async Task BillAsync(PlatformSubscription subscription, PlatformPlan plan, PlatformSubscriptionEvent change, decimal chargeNow = 0m)
    {
        PlatformBillingAdministration billing = Billing();
        PlatformSubscriptionStartedBillingHandler handler = new(_kit.Store, _kit.Store, _kit.Tenants.Tenants, billing, new PlatformInvoiceIssuer(billing), _kit.Tenants.Store);
        PlatformOutboxWriter writer = _kit.Tenants.Outbox();
        writer.Write(PlatformMessageTypes.SubscriptionStarted, subscription.PlatformTenantId, PlatformSubscriptionAdministration.Message(subscription, plan, change, chargeNow));
        PlatformOutboxMessage message = _kit.Tenants.Tenants.Outbox.Last();
        await handler.HandleAsync(message, CancellationToken.None);
    }

    private (PlatformTenant Tenant, PlatformPlan Plan, PlatformSubscription Subscription, PlatformSubscriptionEvent Started) Waiting()
    {
        PlatformTenant tenant = _kit.Tenants.Tenant();
        PlatformPlan plan = _kit.Plan(monthly: 10m);
        PlatformSubscriptionStart start = PlatformSubscription.Start(tenant.Id, plan, PlatformSubscriptionTerm.Monthly, 3, 10, false, null, _kit.Now);
        _kit.Store.Subscriptions.Add(start.Subscription);
        start.Subscription.AddCharge("Onboarding", 100m, "USD", "Contract", Guid.NewGuid(), _kit.Now);
        _billing.Profile = Profile();
        _billing.Rates.Add(PlatformTaxRate.Create("CA", null, 5m, "GST", _kit.Now.AddYears(-1), Guid.NewGuid(), _kit.Now));
        _billing.Rates.Add(PlatformTaxRate.Create("CA", "QC", 10m, "GST+QST", _kit.Now.AddYears(-1), Guid.NewGuid(), _kit.Now));

        return (tenant, plan, start.Subscription, start.Event);
    }

    [Fact]
    public async Task FirstPeriod_IsInvoicedOnce_WithSetupFeeChargesAndRegionTax()
    {
        (PlatformTenant tenant, PlatformPlan plan, PlatformSubscription subscription, PlatformSubscriptionEvent started) = Waiting();

        await BillAsync(subscription, plan, started);
        await BillAsync(subscription, plan, started);

        PlatformInvoice invoice = Assert.Single(_billing.Invoices);
        Assert.Equal(PlatformInvoiceKind.FirstPeriod, invoice.Kind);
        Assert.Equal(30m + 50m + 100m, invoice.NetTotal);
        Assert.Equal(18m, invoice.TaxTotal);
        Assert.Equal(198m, invoice.Total);
        Assert.Equal("GST+QST", invoice.Lines[0].TaxLabel);
        Assert.Equal(tenant.DefaultLanguage, invoice.Language);
        Assert.Equal(invoice.IssuedUtc, invoice.DueUtc);
        Assert.StartsWith("INV-2026-", invoice.Number);
        Assert.Empty(subscription.PendingCharges);
        Assert.Single(_billing.Documents, document => document.Kind == PlatformBillingDocumentKind.Invoice);
        Assert.Contains(_kit.Tenants.Tenants.Outbox, message => message.Type == PlatformMessageTypes.BillingDocumentIssued);
    }

    [Fact]
    public async Task NoProfile_RefusesToInvoice_SoTheOutboxRetries()
    {
        (_, PlatformPlan plan, PlatformSubscription subscription, PlatformSubscriptionEvent started) = Waiting();
        _billing.Profile = null;

        DomainException refused = await Assert.ThrowsAsync<DomainException>(() => BillAsync(subscription, plan, started));

        Assert.Equal("platform.billing.profile_missing", refused.Error.Code);
        Assert.Empty(_billing.Invoices);
    }

    [Fact]
    public async Task PayingTheWaitingInvoice_ActivatesTheSubscription_AndIssuesAReceipt()
    {
        (_, PlatformPlan plan, PlatformSubscription subscription, PlatformSubscriptionEvent started) = Waiting();
        await BillAsync(subscription, plan, started);
        PlatformInvoice invoice = _billing.Invoices.Single();
        RecordPlatformPaymentUseCase useCase = new(Billing(), _kit.Subscriptions(), _kit.Tenants.Store);

        RecordPlatformPaymentResponse partly = await useCase.ExecuteAsync(
            _kit.Staff(PlatformPermissions.BillingRecordPayments),
            new RecordPlatformPaymentRequest { PlatformInvoiceId = invoice.Id, Amount = 98m, Method = PlatformPaymentMethod.BankTransfer, ReceivedUtc = _kit.Now, Reference = "WIRE 1" },
            CancellationToken.None);
        Assert.True(partly.Succeeded);
        Assert.Equal(PlatformSubscriptionStatus.PendingPayment, subscription.Status);

        RecordPlatformPaymentResponse rest = await useCase.ExecuteAsync(
            _kit.Staff(PlatformPermissions.BillingRecordPayments),
            new RecordPlatformPaymentRequest { PlatformInvoiceId = invoice.Id, Amount = 100m, Method = PlatformPaymentMethod.Cash, ReceivedUtc = _kit.Now },
            CancellationToken.None);

        Assert.True(rest.Succeeded);
        Assert.Equal(PlatformSubscriptionStatus.Active, subscription.Status);
        Assert.Equal(PlatformInvoiceStatus.Paid, invoice.Status);
        Assert.Equal(2, _billing.Documents.Count(document => document.Kind == PlatformBillingDocumentKind.Receipt));
        Assert.Contains(_kit.Tenants.Tenants.Outbox, message => message.Type == PlatformMessageTypes.InvoicePaid);
        Assert.Contains(_kit.Store.Events, item => item.EventCode == PlatformSubscriptionEventCodes.InvoicePaid);
    }

    [Fact]
    public async Task CardPayments_CannotBeTypedByStaff()
    {
        (_, PlatformPlan plan, PlatformSubscription subscription, PlatformSubscriptionEvent started) = Waiting();
        await BillAsync(subscription, plan, started);

        RecordPlatformPaymentResponse response = await new RecordPlatformPaymentUseCase(Billing(), _kit.Subscriptions(), _kit.Tenants.Store).ExecuteAsync(
            _kit.Staff(PlatformPermissions.BillingRecordPayments),
            new RecordPlatformPaymentRequest { PlatformInvoiceId = _billing.Invoices.Single().Id, Amount = 10m, Method = PlatformPaymentMethod.Card, ReceivedUtc = _kit.Now, Reference = "x" },
            CancellationToken.None);

        Assert.Equal("platform.billing.method_not_allowed", response.Error!.Code);
    }

    [Fact]
    public async Task CreditNote_IsDecidedBySomeoneElse()
    {
        (_, PlatformPlan plan, PlatformSubscription subscription, PlatformSubscriptionEvent started) = Waiting();
        await BillAsync(subscription, plan, started);
        PlatformInvoice invoice = _billing.Invoices.Single();
        var clerk = _kit.Staff(PlatformPermissions.BillingRequestCredits, PlatformPermissions.BillingApproveCredits);

        Assert.True((await new RequestPlatformCreditNoteUseCase(Billing(), _kit.Tenants.Store).ExecuteAsync(
            clerk, new RequestPlatformCreditNoteRequest { PlatformInvoiceId = invoice.Id, Amount = 20m, Reason = "Outage" }, CancellationToken.None)).Succeeded);
        Assert.Contains(_kit.Tenants.Tenants.Outbox, message => message.Type == PlatformMessageTypes.ApprovalRequested);

        DecidePlatformCreditNoteRequest decide = new() { PlatformInvoiceId = invoice.Id, CreditId = invoice.PendingCredit!.Id, Approve = true };
        DecidePlatformCreditNoteResponse own = await new DecidePlatformCreditNoteUseCase(Billing(), _kit.Tenants.Store).ExecuteAsync(clerk, decide, CancellationToken.None);
        Assert.Equal("platform.billing.own_request", own.Error!.Code);

        DecidePlatformCreditNoteResponse manager = await new DecidePlatformCreditNoteUseCase(Billing(), _kit.Tenants.Store).ExecuteAsync(
            _kit.Staff(PlatformPermissions.BillingApproveCredits), decide, CancellationToken.None);

        Assert.True(manager.Succeeded);
        Assert.Equal(178m, invoice.Balance);
        Assert.StartsWith("CN-2026-", invoice.Credits.Single().Number);
        Assert.Single(_billing.Documents, document => document.Kind == PlatformBillingDocumentKind.CreditNote);
    }

    [Fact]
    public async Task Clock_MarksAnUnpaidRenewalPastDue_AndItsSubscription()
    {
        PlatformTenant tenant = _kit.Tenants.Tenant();
        PlatformPlan plan = _kit.Plan(monthly: 10m);
        PlatformSubscription subscription = _kit.Subscribe(tenant.Id, plan);
        _billing.Profile = Profile();
        PlatformBillingSettings settings = PlatformBillingSettings.Create(_kit.Now);
        settings.Update(7, "INV", "CN", "RCT", PlatformPaperSize.A4, true, new Dictionary<Vixly.Shared.Kernel.Localization.LanguageCode, (string?, string?)>(), Guid.NewGuid(), _kit.Now);
        _billing.Settings = settings;
        PlatformInvoice renewal = await new PlatformInvoiceIssuer(Billing()).IssueAsync(
            tenant, subscription.Id, PlatformInvoiceKind.Renewal, "renewal:1", "USD", null, null,
            new[] { new PlatformInvoiceDraftLine(PlatformInvoiceLineKind.PlanPeriod, "ui.invoice_line.plan_period", new Dictionary<string, string>(), 3m, 10m) },
            CancellationToken.None);

        _kit.Tenants.Clock.Advance(TimeSpan.FromDays(8));
        int marked = await new PlatformBillingClock(Billing(), _kit.Subscriptions(), _kit.Tenants.Store).ProcessOverdueAsync(CancellationToken.None);

        Assert.Equal(1, marked);
        Assert.NotNull(renewal.PastDueUtc);
        Assert.Equal(PlatformSubscriptionStatus.PastDue, subscription.Status);
    }
}
