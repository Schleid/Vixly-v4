using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Application.UseCases.Payments.StartPlatformLinkCheckout;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformBankDebitCheckoutTests.cs
//
// PURPOSE:
// Proves the checkout choices and the delayed bank payments on the shared Stripe account (2026-11-02).
//
// RESPONSIBILITIES:
// - Registration and card saving take cards only; pay links also take a US bank account
// - Branding and the statement suffix from Payments settings reach the provider
// - A bank debit is processing until it settles: the invoice is paid only then, as a bank debit; a failure fails it
// - A wallet payment is noted on the charge
//
// IMPORTANT NOTES:
// - Events go through the real webhook use case and outbox handler (PaymentsTestKit)
// =========================================================
public sealed class PlatformBankDebitCheckoutTests
{
    private readonly PaymentsTestKit _kit = new();

    private async Task<PlatformCardTransaction> PayLinkAsync(PlatformInvoice invoice, PlatformTenant tenant, bool saveCard = false)
    {
        PlatformPaymentsAdministration payments = _kit.Administration();
        (string token, _) = payments.IssueLink(PlatformPaymentLinkKind.PayInvoice, tenant.Id, invoice.Id, null);
        await payments.SaveAsync(CancellationToken.None);
        StartPlatformLinkCheckoutResponse response = await new StartPlatformLinkCheckoutUseCase(_kit.Administration()).ExecuteAsync(
            PlatformAnonymousAuthorizationContext.Instance,
            new StartPlatformLinkCheckoutRequest { Token = token, SaveCard = saveCard },
            CancellationToken.None);
        Assert.True(response.Succeeded, response.Error?.Code);
        return _kit.Payments.Transactions.Last();
    }

    private void Branded()
    {
        _kit.Ready();
        _kit.Payments.Merchant!.UpdateCheckout("Vixly by TechCrafters", "https://vixly.ltd/logo.png", "https://vixly.ltd/icon.png", "#0F172A", "#2563eb", "techcraft", "vixly", Guid.NewGuid(), _kit.Now);
    }

    [Fact]
    public async Task PayLink_OffersBankAccount_WithBrandingAndSuffix()
    {
        Branded();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice(150m);

        await PayLinkAsync(invoice, tenant);

        PlatformCheckoutRequest request = Assert.Single(_kit.Gateway.Checkouts);
        Assert.True(request.AllowBankAccount);
        Assert.False(request.CardOnly);
        Assert.Equal("VIXLY", request.StatementSuffix);
        Assert.Equal(new PlatformCheckoutBranding("Vixly by TechCrafters", "https://vixly.ltd/logo.png", "https://vixly.ltd/icon.png", "#0F172A", "#2563EB"), request.Branding);
    }

    [Fact]
    public async Task PayLink_SavingTheCard_IsCardOnly()
    {
        Branded();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice(150m);

        await PayLinkAsync(invoice, tenant, saveCard: true);

        Assert.False(Assert.Single(_kit.Gateway.Checkouts).AllowBankAccount);
    }

    [Fact]
    public async Task Registration_IsCardOnly()
    {
        Branded();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice(150m, PlatformInvoiceKind.FirstPeriod);

        await _kit.Administration().OpenPaymentCheckoutAsync(tenant, invoice, PlatformCardTransactionOrigin.Registration, false, false, "https://vixly.test/ok", "https://vixly.test/no", CancellationToken.None);

        PlatformCheckoutRequest request = Assert.Single(_kit.Gateway.Checkouts);
        Assert.False(request.AllowBankAccount);
        Assert.True(request.CardOnly);
    }

    [Fact]
    public async Task NoBranding_WhenNothingIsSet()
    {
        _kit.Ready();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice(150m);

        await PayLinkAsync(invoice, tenant);

        Assert.Null(Assert.Single(_kit.Gateway.Checkouts).Branding);
        Assert.Null(_kit.Gateway.Checkouts[0].StatementSuffix);
    }

    [Fact]
    public async Task OffSessionCharge_CarriesTheSuffix()
    {
        Branded();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice(150m);
        PlatformSavedCard card = _kit.SaveCard(tenant.Id, autoCharge: true);
        PlatformCardTransaction charge = PlatformCardTransaction.Charge(tenant.Id, invoice.Id, PlatformCardTransactionOrigin.Automatic, PlatformCardProvider.Stripe, invoice.Balance, "USD", card.Id, null, _kit.Now);
        _kit.Payments.Transactions.Add(charge);

        await _kit.Administration().ChargeSavedCardAsync(charge, _kit.Payments.Profiles.Single(), card, invoice, CancellationToken.None);

        Assert.Equal("VIXLY", Assert.Single(_kit.Gateway.Charges).StatementSuffix);
    }

    [Fact]
    public async Task BankDebit_Processing_ThenSettled_PaysTheInvoiceAsABankDebit()
    {
        _kit.Ready();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice(150m);
        PlatformCardTransaction charge = await PayLinkAsync(invoice, tenant);

        Assert.True(await _kit.DeliverAsync(PaymentsTestKit.Event(PlatformCardEventKind.CheckoutProcessing, "evt_1", charge.Id, "pi_bank")));

        Assert.True(charge.Delayed);
        Assert.Equal(PlatformCardTransactionStatus.Pending, charge.Status);
        Assert.Equal(PlatformInvoiceStatus.Issued, invoice.Status);
        Assert.Empty(invoice.Payments);

        // Stripe also sends payment_intent.payment_failed / succeeded; the checkout's own event settles.
        Assert.True(await _kit.DeliverAsync(PaymentsTestKit.Event(PlatformCardEventKind.CheckoutPaymentSucceeded, "evt_2", charge.Id, "pi_bank")));
        Assert.True(await _kit.DeliverAsync(PaymentsTestKit.Event(PlatformCardEventKind.PaymentSucceeded, "evt_3", charge.Id, "pi_bank")));

        Assert.Equal(PlatformCardTransactionStatus.Succeeded, charge.Status);
        Assert.Equal(PlatformInvoiceStatus.Paid, invoice.Status);
        PlatformPayment payment = Assert.Single(invoice.Payments);
        Assert.Equal(PlatformPaymentMethod.BankDebit, payment.Method);
        Assert.Null(charge.Wallet);
    }

    [Fact]
    public async Task BankDebit_Failed_FailsTheCharge_InvoiceStaysUnpaid()
    {
        _kit.Ready();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice(150m);
        PlatformCardTransaction charge = await PayLinkAsync(invoice, tenant);

        await _kit.DeliverAsync(PaymentsTestKit.Event(PlatformCardEventKind.CheckoutProcessing, "evt_1", charge.Id, "pi_bank"));
        Assert.True(await _kit.DeliverAsync(PaymentsTestKit.Event(PlatformCardEventKind.CheckoutPaymentFailed, "evt_2", charge.Id, "pi_bank", failure: "bank_payment_failed")));

        Assert.Equal(PlatformCardTransactionStatus.Failed, charge.Status);
        Assert.Equal(PlatformInvoiceStatus.Issued, invoice.Status);
        Assert.Empty(invoice.Payments);
    }

    [Fact]
    public async Task CardDeclinedInsideTheCheckout_StillLetsTheTenantRetry()
    {
        _kit.Ready();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice(150m);
        PlatformCardTransaction charge = await PayLinkAsync(invoice, tenant);

        await _kit.DeliverAsync(PaymentsTestKit.Event(PlatformCardEventKind.PaymentFailed, "evt_1", charge.Id, "pi_card", failure: "card_declined"));

        Assert.True(charge.IsOpen);
        Assert.False(charge.Delayed);
    }

    [Fact]
    public async Task WalletPayment_IsNotedOnTheCharge()
    {
        _kit.Ready();
        _kit.Gateway.Wallet = "apple_pay";
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice(150m);
        PlatformCardTransaction charge = await PayLinkAsync(invoice, tenant);

        await _kit.DeliverAsync(PaymentsTestKit.Event(PlatformCardEventKind.PaymentSucceeded, "evt_1", charge.Id, "pi_9"));

        Assert.Equal("apple_pay", charge.Wallet);
        Assert.Equal(PlatformPaymentMethod.Card, Assert.Single(invoice.Payments).Method);
    }
}
