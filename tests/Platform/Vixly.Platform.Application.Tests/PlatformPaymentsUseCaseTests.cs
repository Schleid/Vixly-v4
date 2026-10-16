using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Application.UseCases.Payments.ChargePlatformInvoiceCard;
using Vixly.Platform.Application.UseCases.Payments.RefundPlatformCardPayment;
using Vixly.Platform.Application.UseCases.Payments.StartPlatformLinkCheckout;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformPaymentsUseCaseTests.cs
//
// PURPOSE:
// Proves Slice 5 card payments end to end over fakes: checkout, webhook, saved cards, staff and automatic charges, refunds, registration.
//
// RESPONSIBILITIES:
// - Only a verified event records a payment, once, with a receipt; excess is refunded
// - One open charge per invoice; failures reach the tenant and Slice 6
// - Refunds limited by approved credit notes; automatic charging only with consent
//
// IMPORTANT NOTES:
// - Tests assert error codes and states, never text (L-1)
// =========================================================
public sealed class PlatformPaymentsUseCaseTests
{
    private readonly PaymentsTestKit _kit = new();

    private async Task<string> CheckoutAsync(PlatformInvoice invoice, PlatformTenant tenant, bool saveCard = false, bool autoCharge = false)
    {
        PlatformPaymentsAdministration payments = _kit.Administration();
        (string token, _) = payments.IssueLink(PlatformPaymentLinkKind.PayInvoice, tenant.Id, invoice.Id, null);
        await payments.SaveAsync(CancellationToken.None);
        StartPlatformLinkCheckoutResponse response = await new StartPlatformLinkCheckoutUseCase(_kit.Administration()).ExecuteAsync(
            PlatformAnonymousAuthorizationContext.Instance,
            new StartPlatformLinkCheckoutRequest { Token = token, SaveCard = saveCard, AutoCharge = autoCharge },
            CancellationToken.None);
        Assert.True(response.Succeeded, response.Error?.Code);
        return response.CheckoutUrl;
    }

    [Fact]
    public async Task PayLink_OpensCheckoutForTheBalance_AndOnlyTheWebhookRecordsThePayment_Once()
    {
        _kit.Ready();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice(150m);

        string url = await CheckoutAsync(invoice, tenant, saveCard: true, autoCharge: true);

        PlatformCardTransaction charge = Assert.Single(_kit.Payments.Transactions);
        Assert.Equal("https://checkout.test/cs_1", url);
        Assert.Equal(15000, _kit.Gateway.Checkouts[0].AmountMinor);
        Assert.Equal(charge.IdempotencyKey, _kit.Gateway.Checkouts[0].IdempotencyKey);
        Assert.Equal(PlatformInvoiceStatus.Issued, invoice.Status);

        Dictionary<string, string> metadata = new(_kit.Gateway.Checkouts[0].Metadata);
        string completed = PaymentsTestKit.Event(PlatformCardEventKind.CheckoutCompleted, "evt_1", charge.Id, "pi_9", metadata: metadata, paymentMethodId: "pm_new");

        Assert.True(await _kit.DeliverAsync(completed));
        Assert.False(await _kit.DeliverAsync(completed));
        Assert.True(await _kit.DeliverAsync(PaymentsTestKit.Event(PlatformCardEventKind.PaymentSucceeded, "evt_2", charge.Id, "pi_9")));

        Assert.Equal(PlatformCardTransactionStatus.Succeeded, charge.Status);
        Assert.Equal(PlatformInvoiceStatus.Paid, invoice.Status);
        PlatformPayment payment = Assert.Single(invoice.Payments);
        Assert.Equal(PlatformPaymentMethod.Card, payment.Method);
        Assert.Equal("pi_9", payment.Reference);
        Assert.Equal(payment.Id, charge.PlatformPaymentId);
        Assert.Single(_kit.Billing.Documents, document => document.Kind == PlatformBillingDocumentKind.Receipt);
        Assert.Contains(_kit.Kit.Tenants.Tenants.Outbox, message => message.Type == PlatformMessageTypes.InvoicePaid);

        PlatformSavedCard card = Assert.Single(_kit.Payments.Cards);
        Assert.Equal("pm_new", card.ProviderPaymentMethodId);
        Assert.True(_kit.Payments.Profiles.Single().AutoCharge);
        Assert.All(_kit.Payments.Events, item => Assert.NotNull(item.ProcessedUtc));
    }

    // As Stripe sends it: checkout completion names no card; the payment event names it, with the checkout's metadata.
    [Fact]
    public async Task PayLink_WithSaveCard_SavesTheCardNamedByThePaymentEvent_Once()
    {
        _kit.Ready();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice(45m);

        await CheckoutAsync(invoice, tenant, saveCard: true);

        PlatformCardTransaction charge = Assert.Single(_kit.Payments.Transactions);
        Dictionary<string, string> metadata = new(_kit.Gateway.Checkouts[0].Metadata);

        Assert.True(await _kit.DeliverAsync(PaymentsTestKit.Event(PlatformCardEventKind.CheckoutCompleted, "evt_c", charge.Id, "pi_5", metadata: metadata)));
        Assert.Empty(_kit.Payments.Cards);
        Assert.True(await _kit.DeliverAsync(PaymentsTestKit.Event(PlatformCardEventKind.PaymentSucceeded, "evt_p", charge.Id, "pi_5", metadata: metadata, paymentMethodId: "pm_pay")));
        Assert.True(await _kit.DeliverAsync(PaymentsTestKit.Event(PlatformCardEventKind.PaymentSucceeded, "evt_p2", charge.Id, "pi_5", metadata: metadata, paymentMethodId: "pm_pay")));

        Assert.Equal(PlatformInvoiceStatus.Paid, invoice.Status);
        PlatformSavedCard card = Assert.Single(_kit.Payments.Cards);
        Assert.Equal("pm_pay", card.ProviderPaymentMethodId);
        Assert.Equal(PlatformCardAddedVia.PayLink, card.AddedVia);
        Assert.False(_kit.Payments.Profiles.Single().AutoCharge);
    }

    [Fact]
    public async Task Webhook_WithABadSignature_IsRefused_AndNothingIsStored()
    {
        var response = await new Vixly.Platform.Application.UseCases.Payments.ReceivePlatformProviderEvent.ReceivePlatformProviderEventUseCase(_kit.Administration(), _kit.Kit.Tenants.Outbox()).ExecuteAsync(
            PlatformAnonymousAuthorizationContext.Instance,
            new Vixly.Platform.Application.UseCases.Payments.ReceivePlatformProviderEvent.ReceivePlatformProviderEventRequest { Payload = "{}", Signature = "forged" },
            CancellationToken.None);

        Assert.Equal("platform.payments.event_invalid", response.Error!.Code);
        Assert.Empty(_kit.Payments.Events);
    }

    [Fact]
    public async Task AbandonedCheckout_GivesWayToANewOne_ButNeverTwoOpenCharges()
    {
        _kit.Ready();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice();

        await CheckoutAsync(invoice, tenant);
        await CheckoutAsync(invoice, tenant);

        Assert.Equal(new[] { "cs_1" }, _kit.Gateway.Expired);
        Assert.Equal(1, _kit.Payments.Transactions.Count(item => item.IsOpen));
        Assert.Equal(PlatformCardTransactionStatus.Cancelled, _kit.Payments.Transactions[0].Status);
    }

    [Fact]
    public async Task CardPayments_AreRefused_WhenNotReadyOrTheCurrencyIsNotAccepted()
    {
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice();
        PlatformPaymentsAdministration payments = _kit.Administration();

        Assert.Equal("platform.payments.not_ready", (await payments.RefusalForAsync("USD", CancellationToken.None))!.Code);
        _kit.Ready();
        Assert.Equal("platform.payments.currency_not_accepted", (await payments.RefusalForAsync("EUR", CancellationToken.None))!.Code);
        _kit.Gateway.Status = new PlatformCardGatewayStatus(PlatformCardProvider.Stripe, true, PlatformPaymentMode.Live, null);
        Assert.Equal("platform.payments.not_ready", (await payments.RefusalForAsync("USD", CancellationToken.None))!.Code);
    }

    [Fact]
    public async Task StaffCharge_NeedsAValidCard_SendsTheBalanceOnce_AndAFailureReachesTheTenant()
    {
        _kit.Ready();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice(80m);
        var staff = _kit.Kit.Staff(PlatformPermissions.BillingChargeCards);
        ChargePlatformInvoiceCardUseCase useCase = new(_kit.Administration());
        ChargePlatformInvoiceCardRequest request = new() { PlatformInvoiceId = invoice.Id };

        Assert.Equal("platform.payments.no_saved_card", (await useCase.ExecuteAsync(staff, request, CancellationToken.None)).Error!.Code);

        _kit.SaveCard(tenant.Id);
        _kit.Gateway.NextCharge = new PlatformCardResult("pi_7", PlatformCardOutcome.Failed, "card_declined");
        ChargePlatformInvoiceCardResponse declined = await new ChargePlatformInvoiceCardUseCase(_kit.Administration()).ExecuteAsync(staff, request, CancellationToken.None);

        Assert.Equal(PlatformCardTransactionStatus.Failed, declined.Status);
        Assert.Equal("card_declined", declined.FailureCode);
        Assert.Equal(8000, _kit.Gateway.Charges.Single().AmountMinor);
        Assert.Contains(_kit.Billing.Events, item => item.EventCode == PlatformInvoiceEventCodes.CardFailed);
        Assert.Contains(_kit.Kit.Tenants.Tenants.Outbox, message => message.Type == PlatformMessageTypes.CardPaymentFailed);

        _kit.Gateway.NextCharge = new PlatformCardResult("pi_8", PlatformCardOutcome.Succeeded, null);
        ChargePlatformInvoiceCardResponse sent = await new ChargePlatformInvoiceCardUseCase(_kit.Administration()).ExecuteAsync(staff, request, CancellationToken.None);
        Assert.Equal(PlatformCardTransactionStatus.Pending, sent.Status);
        Assert.Equal(PlatformInvoiceStatus.Issued, invoice.Status);

        Assert.Equal("platform.payments.charge_open", (await new ChargePlatformInvoiceCardUseCase(_kit.Administration()).ExecuteAsync(staff, request, CancellationToken.None)).Error!.Code);
        Assert.Equal(2, _kit.Gateway.Charges.Select(item => item.IdempotencyKey).Distinct().Count());
    }

    [Fact]
    public async Task ChargeConfirmedAfterTheInvoiceWasPaidByHand_RefundsTheExcess()
    {
        _kit.Ready();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice(100m);
        await CheckoutAsync(invoice, tenant);
        PlatformCardTransaction charge = _kit.Payments.Transactions.Single();
        invoice.RecordPayment("RCT-2026-000001", 60m, PlatformPaymentMethod.BankTransfer, _kit.Now, "WIRE", null, Guid.NewGuid(), _kit.Now);

        await _kit.DeliverAsync(PaymentsTestKit.Event(PlatformCardEventKind.PaymentSucceeded, "evt_1", charge.Id, "pi_5"));

        Assert.Equal(PlatformInvoiceStatus.Paid, invoice.Status);
        Assert.Equal(40m, invoice.Payments.Single(payment => payment.Method == PlatformPaymentMethod.Card).Amount);
        PlatformCardTransaction excess = Assert.Single(_kit.Payments.Transactions, item => item.Origin == PlatformCardTransactionOrigin.ExcessRefund);
        Assert.Equal(60m, excess.Amount);

        int sent = await new PlatformPaymentsClock(_kit.Administration(), _kit.Settlement(_kit.Administration()), _kit.Payments).RunAsync(CancellationToken.None);
        Assert.True(sent >= 1);
        Assert.Equal(6000, _kit.Gateway.Refunds.Single().AmountMinor);
        Assert.Equal(PlatformCardTransactionStatus.Pending, excess.Status);
    }

    [Fact]
    public async Task Refund_OnlyAgainstAnIssuedCreditNote_UpToWhatWasCharged_AndLowersRefundOwedWhenConfirmed()
    {
        _kit.Ready();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice(100m);
        await CheckoutAsync(invoice, tenant);
        PlatformCardTransaction charge = _kit.Payments.Transactions.Single();
        await _kit.DeliverAsync(PaymentsTestKit.Event(PlatformCardEventKind.PaymentSucceeded, "evt_1", charge.Id, "pi_5"));
        var staff = _kit.Kit.Staff(PlatformPermissions.BillingRefundCards);
        RefundPlatformCardPaymentUseCase useCase = new(_kit.Administration());

        Assert.Equal("platform.payments.refund_not_allowed", (await useCase.ExecuteAsync(staff, new RefundPlatformCardPaymentRequest { PlatformInvoiceId = invoice.Id, CreditId = Guid.NewGuid(), Amount = 10m }, CancellationToken.None)).Error!.Code);

        invoice.RequestCredit(30m, false, "Outage", Guid.NewGuid(), _kit.Now);
        Guid creditId = invoice.PendingCredit!.Id;
        invoice.DecideCredit(creditId, true, "CN-2026-000001", null, Guid.NewGuid(), _kit.Now);

        Assert.Equal("platform.payments.refund_above_charged", (await new RefundPlatformCardPaymentUseCase(_kit.Administration()).ExecuteAsync(staff, new RefundPlatformCardPaymentRequest { PlatformInvoiceId = invoice.Id, CreditId = creditId, Amount = 31m }, CancellationToken.None)).Error!.Code);
        Assert.True((await new RefundPlatformCardPaymentUseCase(_kit.Administration()).ExecuteAsync(staff, new RefundPlatformCardPaymentRequest { PlatformInvoiceId = invoice.Id, CreditId = creditId, Amount = 30m }, CancellationToken.None)).Succeeded);

        PlatformCardTransaction refund = Assert.Single(_kit.Payments.Transactions, item => item.Kind == PlatformCardTransactionKind.Refund);
        Assert.Equal("pi_5", _kit.Gateway.Refunds.Single().PaymentId);
        Assert.Equal(30m, invoice.RefundOwed);

        await _kit.DeliverAsync(PaymentsTestKit.Event(PlatformCardEventKind.RefundSucceeded, "evt_r", refund.Id, refundId: "re_1"));

        Assert.Equal(PlatformCardTransactionStatus.Succeeded, refund.Status);
        Assert.Equal(0m, invoice.RefundOwed);
        Assert.Contains(_kit.Billing.Events, item => item.EventCode == PlatformInvoiceEventCodes.CardRefunded);
    }

    [Fact]
    public async Task Clock_ChargesOnlyConsentingTenants_Once()
    {
        _kit.Ready();
        (PlatformTenant consenting, PlatformInvoice due) = _kit.OpenInvoice(20m);
        (PlatformTenant other, PlatformInvoice notConsented) = _kit.OpenInvoice(30m);
        _kit.SaveCard(consenting.Id, autoCharge: true);
        _kit.SaveCard(other.Id, autoCharge: false);
        _kit.Gateway.NextCharge = new PlatformCardResult("pi_auto", PlatformCardOutcome.Pending, null);

        PlatformPaymentsClock clock = new(_kit.Administration(), _kit.Settlement(_kit.Administration()), _kit.Payments);
        await clock.RunAsync(CancellationToken.None);
        await new PlatformPaymentsClock(_kit.Administration(), _kit.Settlement(_kit.Administration()), _kit.Payments).RunAsync(CancellationToken.None);

        PlatformCardChargeRequest charged = Assert.Single(_kit.Gateway.Charges);
        Assert.Equal(2000, charged.AmountMinor);
        Assert.DoesNotContain(_kit.Payments.Transactions, item => item.PlatformInvoiceId == notConsented.Id);
        Assert.Equal(PlatformCardTransactionOrigin.Automatic, _kit.Payments.Transactions.Single().Origin);
    }

    [Fact]
    public async Task Clock_AsksTheProviderAboutStaleCharges_AndAppliesAConfirmedOne()
    {
        _kit.Ready();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice(50m);
        await CheckoutAsync(invoice, tenant);
        _kit.Kit.Tenants.Clock.Advance(TimeSpan.FromMinutes(31));
        _kit.Gateway.NextLookup = new PlatformCardResult("pi_late", PlatformCardOutcome.Succeeded, null);

        await new PlatformPaymentsClock(_kit.Administration(), _kit.Settlement(_kit.Administration()), _kit.Payments).RunAsync(CancellationToken.None);

        Assert.Equal(PlatformInvoiceStatus.Paid, invoice.Status);
        Assert.Equal("pi_late", invoice.Payments.Single().Reference);
    }

    [Fact]
    public async Task InvoicePaid_MovesTheRegistrationToAccountSetup()
    {
        PlatformTenantRegistration registration = PlatformTenantRegistration.Start(
            TenantTestKit.Company(), "Grace Hopper", Vixly.Shared.Kernel.ValueObjects.EmailAddress.Create("grace@northwind.example"), Vixly.Shared.Kernel.ValueObjects.PhoneNumber.Create("+1 514 555 0101"),
            Vixly.Shared.Kernel.Localization.LanguageCode.Es, "America/Toronto", "Northwind", new string('a', 64), true, "2026-10", "2026-09", "203.0.113.7", _kit.Now);
        Guid tenantId = Guid.NewGuid();
        registration.MarkEmailVerified(tenantId, _kit.Now);
        registration.RecordPlanChosen(false, _kit.Now);
        _kit.Kit.Tenants.Tenants.Registrations.Add(registration);
        PlatformOutboxWriter outbox = _kit.Kit.Tenants.Outbox();
        outbox.Write(PlatformMessageTypes.InvoicePaid, tenantId, new PlatformInvoicePaid(Guid.NewGuid(), "INV-2026-000001", tenantId, null, 10m, "USD", _kit.Now));

        await new PlatformRegistrationPaidHandler(_kit.Kit.Tenants.Tenants, _kit.Kit.Tenants.Audit(), _kit.Kit.Tenants.Store, _kit.Kit.Tenants.Clock)
            .HandleAsync(_kit.Kit.Tenants.Tenants.Outbox.Last(), CancellationToken.None);

        Assert.Equal(PlatformTenantRegistrationStatus.PendingAccountSetup, registration.Status);
    }
}
