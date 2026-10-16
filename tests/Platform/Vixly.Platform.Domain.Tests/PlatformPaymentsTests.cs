using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformPaymentsTests.cs
//
// PURPOSE:
// Proves card payments in the domain (Slice 5): merchant account, saved cards, transactions, links, refunds and disputes.
//
// RESPONSIBILITIES:
// - Merchant account validation and readiness
// - Saved card data and expiry; transaction states that cannot charge twice
// - Refund limits, invoice refund owed, lost dispute, registration paid
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformPaymentsTests
{
    private static readonly DateTimeOffset Now = SubscriptionTestData.Now;

    private static readonly Guid Staff = Guid.Parse("0199e3a0-0000-7000-8000-000000000021");

    private static PlatformInvoice Invoice(decimal price = 100m)
    {
        return PlatformInvoice.Issue(
            SubscriptionTestData.Tenant,
            Guid.NewGuid(),
            PlatformInvoiceKind.Renewal,
            "INV-2026-000001",
            "source-" + Guid.NewGuid().ToString("N"),
            "USD",
            LanguageCode.Default,
            PlatformInvoiceParty.Create("Atlas Freight LLC", "1 Dock Road", null, "billing@atlas.test"),
            0,
            Now,
            Now.AddMonths(1),
            new[] { new PlatformInvoiceLineInput(PlatformInvoiceLineKind.PlanPeriod, "ui.invoice_line.test", new Dictionary<string, string>(StringComparer.Ordinal), 1m, price, 0m, null) },
            Now).Invoice;
    }

    private static PlatformCardTransaction SucceededCharge(PlatformInvoice invoice, decimal amount)
    {
        PlatformCardTransaction charge = PlatformCardTransaction.Charge(invoice.PlatformTenantId, invoice.Id, PlatformCardTransactionOrigin.PayLink, PlatformCardProvider.Stripe, amount, "USD", null, null, Now);
        charge.Succeed("pi_123", Now);
        return charge;
    }

    [Fact]
    public void MerchantAccount_NeedsMatchingKeyAndCurrencies_AndACheckBeforeItIsReady()
    {
        PlatformMerchantAccount account = PlatformMerchantAccount.Create(PlatformCardProvider.Stripe, Now);

        TenantTestData.AssertError(PlatformPaymentsErrors.PublishableKeyInvalid,
            () => account.Update(PlatformCardProvider.Stripe, PlatformPaymentMode.Live, "pk_test_abc", new[] { "usd" }, true, Staff, Now));
        TenantTestData.AssertError(PlatformPaymentsErrors.NotReady,
            () => account.Update(PlatformCardProvider.Stripe, PlatformPaymentMode.Test, "pk_test_abc", Array.Empty<string>(), true, Staff, Now));
        TenantTestData.AssertError(PlatformPaymentsErrors.CurrencyInvalid,
            () => account.Update(PlatformCardProvider.Stripe, PlatformPaymentMode.Test, "pk_test_abc", new[] { "dollars" }, false, Staff, Now));

        account.Update(PlatformCardProvider.Stripe, PlatformPaymentMode.Test, "pk_test_abc", new[] { "usd", "EUR", "USD" }, true, Staff, Now);
        Assert.Equal(new[] { "EUR", "USD" }, account.Currencies);
        Assert.False(account.Accepts("USD"));

        account.RecordCheck(true, null, Now);
        Assert.True(account.Accepts("USD"));
        Assert.False(account.Accepts("CAD"));

        account.Update(PlatformCardProvider.Stripe, PlatformPaymentMode.Live, "pk_live_abc", new[] { "USD" }, true, Staff, Now);
        Assert.False(account.IsReady);
    }

    [Fact]
    public void SavedCard_KeepsOnlyReferenceBrandLastFourAndExpiry_AndExpiresAfterItsMonth()
    {
        TenantTestData.AssertError(PlatformPaymentsErrors.CardInvalid,
            () => PlatformSavedCard.Create(Guid.NewGuid(), PlatformCardProvider.Stripe, "pm_1", "visa", "42424", 12, 2027, PlatformCardAddedVia.PayLink, Now));
        TenantTestData.AssertError(PlatformPaymentsErrors.ProviderReferenceInvalid,
            () => PlatformSavedCard.Create(Guid.NewGuid(), PlatformCardProvider.Stripe, " ", "visa", "4242", 12, 2027, PlatformCardAddedVia.PayLink, Now));

        PlatformSavedCard card = PlatformSavedCard.Create(Guid.NewGuid(), PlatformCardProvider.Stripe, "pm_1", "Visa", "4242", 10, 2026, PlatformCardAddedVia.SaveCardLink, Now);

        Assert.Equal("visa", card.Brand);
        Assert.True(card.IsDefault);
        Assert.False(card.IsExpiredAt(new DateTimeOffset(2026, 10, 31, 23, 0, 0, TimeSpan.Zero)));
        Assert.True(card.IsExpiredAt(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero)));

        card.Remove(Staff, Now);
        Assert.False(card.IsDefault);
        TenantTestData.AssertError(PlatformPaymentsErrors.CardRemoved, () => card.Remove(Staff, Now));
    }

    [Fact]
    public void Charge_SucceedsOnce_CanSucceedAfterAFailedTry_ButNeverAfterCancel()
    {
        PlatformInvoice invoice = Invoice();
        PlatformCardTransaction charge = PlatformCardTransaction.Charge(invoice.PlatformTenantId, invoice.Id, PlatformCardTransactionOrigin.PayLink, PlatformCardProvider.Stripe, 100m, "usd", null, null, Now);

        Assert.True(charge.IsOpen);
        Assert.StartsWith("vx4-", charge.IdempotencyKey);
        Assert.Equal(10000, charge.MinorUnits);
        Assert.True(charge.Fail("pi_1", "card_declined", Now));
        Assert.True(charge.Succeed("pi_1", Now));
        Assert.False(charge.Succeed("pi_1", Now));
        Assert.False(charge.Fail("pi_1", "late", Now));
        Assert.Equal(PlatformCardTransactionStatus.Succeeded, charge.Status);

        PlatformCardTransaction other = PlatformCardTransaction.Charge(invoice.PlatformTenantId, invoice.Id, PlatformCardTransactionOrigin.Automatic, PlatformCardProvider.Stripe, 100m, "USD", Guid.NewGuid(), null, Now);
        Assert.True(other.Cancel(Now));
        TenantTestData.AssertError(PlatformPaymentsErrors.TransactionClosed, () => other.Succeed("pi_2", Now));
    }

    [Fact]
    public void Amounts_GoToTheProviderInMinorUnits_AndRefundsNeverExceedTheCharge()
    {
        Assert.Equal(1999, PlatformPaymentsLimits.ToMinorUnits(19.99m, "USD"));
        Assert.Equal(500, PlatformPaymentsLimits.ToMinorUnits(500m, "JPY"));
        Assert.Equal(19.99m, PlatformPaymentsLimits.FromMinorUnits(1999, "USD"));
        TenantTestData.AssertError(PlatformPaymentsErrors.AmountInvalid, () => PlatformPaymentsLimits.ToMinorUnits(0.5m, "JPY"));

        PlatformInvoice invoice = Invoice();
        PlatformCardTransaction open = PlatformCardTransaction.Charge(invoice.PlatformTenantId, invoice.Id, PlatformCardTransactionOrigin.StaffCharge, PlatformCardProvider.Stripe, 100m, "USD", null, Staff, Now);
        TenantTestData.AssertError(PlatformPaymentsErrors.RefundNotAllowed, () => PlatformCardTransaction.Refund(open, 10m, null, PlatformCardTransactionOrigin.Refund, Staff, Now));

        PlatformCardTransaction charge = SucceededCharge(invoice, 100m);
        TenantTestData.AssertError(PlatformPaymentsErrors.RefundAboveCharged, () => PlatformCardTransaction.Refund(charge, 100.01m, null, PlatformCardTransactionOrigin.Refund, Staff, Now));
        PlatformCardTransaction refund = PlatformCardTransaction.Refund(charge, 40m, Guid.NewGuid(), PlatformCardTransactionOrigin.Refund, Staff, Now);

        Assert.Equal(PlatformCardTransactionKind.Refund, refund.Kind);
        Assert.Equal(charge.Id, refund.ChargeTransactionId);
        Assert.True(refund.Succeed("re_1", Now));
        Assert.Equal("re_1", refund.ProviderRefundId);
    }

    [Fact]
    public void CardRefund_LowersRefundOwed_AndCannotExceedIt()
    {
        PlatformInvoice invoice = Invoice();
        invoice.RecordPayment("RCT-2026-000001", 100m, PlatformPaymentMethod.Card, Now, "pi_1", null, null, Now);
        invoice.RequestCredit(30m, false, "Outage", Staff, Now);
        invoice.DecideCredit(invoice.PendingCredit!.Id, true, "CN-2026-000001", null, Guid.NewGuid(), Now);
        Assert.Equal(30m, invoice.RefundOwed);

        TenantTestData.AssertError(PlatformBillingErrors.AmountAboveBalance, () => invoice.RecordCardRefund(31m, "re_1", Now));
        PlatformInvoiceEvent refunded = invoice.RecordCardRefund(30m, "re_1", Now);

        Assert.Equal(0m, invoice.RefundOwed);
        Assert.Equal(PlatformInvoiceEventCodes.CardRefunded, refunded.EventCode);
    }

    [Fact]
    public void LostDispute_ReversesTheCardPayment_AndReopensTheBalance()
    {
        PlatformInvoice invoice = Invoice();
        PlatformInvoicePaymentResult paid = invoice.RecordPayment("RCT-2026-000001", 100m, PlatformPaymentMethod.Card, Now, "pi_1", null, null, Now);
        Assert.Equal(PlatformInvoiceStatus.Paid, invoice.Status);

        invoice.RecordDispute(paid.Payment.Id, PlatformInvoiceEventCodes.Disputed, Now);
        Assert.Equal(PlatformInvoiceStatus.Paid, invoice.Status);

        invoice.RecordDispute(paid.Payment.Id, PlatformInvoiceEventCodes.DisputeLost, Now);
        Assert.Equal(PlatformInvoiceStatus.Issued, invoice.Status);
        Assert.Equal(100m, invoice.Balance);
        Assert.Equal(PlatformPaymentStatus.Reversed, paid.Payment.Status);
    }

    [Fact]
    public void Links_StoreOnlyAHash_ExpireAndAreUsedOnce()
    {
        TenantTestData.AssertError(PlatformPaymentsErrors.LinkInvalid,
            () => PlatformPaymentLink.Create(PlatformPaymentLinkKind.PayInvoice, Guid.NewGuid(), null, new string('a', 64), null, Now));
        TenantTestData.AssertError(PlatformPaymentsErrors.LinkInvalid,
            () => PlatformPaymentLink.Create(PlatformPaymentLinkKind.SaveCard, Guid.NewGuid(), null, "short", Staff, Now));

        PlatformPaymentLink link = PlatformPaymentLink.Create(PlatformPaymentLinkKind.SaveCard, Guid.NewGuid(), null, new string('b', 64), Staff, Now);

        Assert.True(link.IsUsableAt(Now.AddDays(6)));
        Assert.False(link.IsUsableAt(Now.AddDays(7)));
        link.MarkUsed(Now);
        Assert.False(link.IsUsableAt(Now));
    }

    [Fact]
    public void Consent_IsGivenByTheTenant_AndStaffCanOnlyTurnItOff()
    {
        PlatformTenantCardProfile profile = PlatformTenantCardProfile.Create(Guid.NewGuid(), PlatformCardProvider.Stripe, PlatformPaymentMode.Test, "cus_1", Now);

        TenantTestData.AssertError(PlatformPaymentsErrors.AutoChargeOff, () => profile.TurnOffAutoCharge(Staff, Now));
        TenantTestData.AssertError(PlatformPaymentsErrors.ConsentInvalid, () => profile.GiveConsent("not-an-email", "2026-10", Now));

        profile.GiveConsent("owner@atlas.test", "2026-10", Now);
        Assert.True(profile.AutoCharge);
        profile.TurnOffAutoCharge(Staff, Now);
        Assert.False(profile.AutoCharge);
        Assert.Equal(Staff, profile.AutoChargeOffByUserId);
    }

    [Fact]
    public void ProviderEvent_NeedsAnIdAndType()
    {
        TenantTestData.AssertError(PlatformPaymentsErrors.EventInvalid, () => PlatformProviderEvent.Receive(PlatformCardProvider.Stripe, "evt_1", " ", null, Now));
        PlatformProviderEvent received = PlatformProviderEvent.Receive(PlatformCardProvider.Stripe, "evt_1", "checkout.session.completed", "cs_1", Now);

        received.MarkProcessed("applied", Now);
        Assert.Equal("applied", received.Outcome);
    }

    [Fact]
    public void Registration_MovesToAccountSetup_WhenItsInvoiceIsPaid_Once()
    {
        PlatformTenantRegistration registration = TenantTestData.Registration();
        registration.MarkEmailVerified(Guid.NewGuid(), Now.AddHours(1));
        registration.RecordPlanChosen(false, Now.AddHours(1));

        Assert.Equal(PlatformTenantRegistrationStatus.PendingPayment, registration.Status);
        Assert.True(registration.RecordPaid(Now.AddHours(2)));
        Assert.Equal(PlatformTenantRegistrationStatus.PendingAccountSetup, registration.Status);
        Assert.False(registration.RecordPaid(Now.AddHours(3)));
    }
}
