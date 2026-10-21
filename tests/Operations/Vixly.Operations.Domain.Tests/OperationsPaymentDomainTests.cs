using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Charges;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsPaymentDomainTests.cs
//
// PURPOSE:
// Proves the cashier rules (Slice 5F step 2, F-4 and C-3 to C-9).
//
// RESPONSIBILITIES:
// - Paid in full with one or more tenders; cash into an open drawer with its change
// - Zelle waits for finance; received or not received
// - Drawers close with a counted difference and a note
// - Refunds after a credit note; no void once paid
//
// IMPORTANT NOTES:
// - Pure domain; no database
// =========================================================
public sealed class OperationsPaymentDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly Guid Warehouse = Guid.NewGuid();

    private static readonly Guid Cashier = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    // Freight 40.00, no tax: 40.00 owed.
    private static OperationsInvoice Invoice()
    {
        OperationsInvoiceLine[] lines = { new(OperationsChargeLineKind.Freight, null, null, null, 1, 1m, 10m, 4m, 40.00m, 0m, 0m, OperationsTaxKind.Freight, true) };
        return OperationsInvoice.Issue(Tenant, "INV1", new OperationsInvoiceDraft(OperationsInvoiceKind.Shipment, Guid.NewGuid(), Warehouse, Guid.NewGuid(), null, "SH1", null, null, "USD", "en",
            new OperationsInvoiceParty("Galaxy Freight", null, "Miami", "C1", "Ana", null, "+1", "a@example.com"), lines, null, false, OperationsTaxRates.None, null), DateOnly.MinValue, Now);
    }

    private static OperationsCashDrawer Drawer() => OperationsCashDrawer.Open(Tenant, Warehouse, Cashier, 100m, Now);

    private static OperationsPayment Pay(OperationsInvoice invoice, OperationsCashDrawer? drawer, bool terms, params OperationsTenderDraft[] tenders) =>
        OperationsPayment.Take(Tenant, invoice, "PAY1", tenders, drawer, terms, Cashier, Guid.NewGuid(), Now);

    [Fact]
    public void Cash_IsTakenInFull_IntoTheOpenDrawer_WithTheChange()
    {
        OperationsInvoice invoice = Invoice();
        OperationsCashDrawer drawer = Drawer();

        OperationsPayment payment = Pay(invoice, drawer, false, new OperationsTenderDraft(OperationsPaymentMethod.Cash, 40m, 50m, null));

        Assert.Equal((40m, 10m, false), (payment.Total, payment.Change, payment.Pending));
        Assert.Equal((OperationsPaymentState.Paid, 0m, 40m), (invoice.PaymentState, invoice.Owed, invoice.PaidTotal));
        Assert.Equal((40m, 140m), (drawer.CashIn, drawer.Expected));
        Assert.False(invoice.CanVoid);
        Assert.Equal(OperationsPaymentErrors.NothingOwed, Assert.Throws<DomainException>(() => Pay(invoice, drawer, false, new OperationsTenderDraft(OperationsPaymentMethod.Cash, 1m, null, null))).Error);
    }

    [Fact]
    public void APayment_MustBeInFull_AndCashNeedsTheCashiersOpenDrawer()
    {
        OperationsInvoice invoice = Invoice();
        OperationsCashDrawer drawer = Drawer();

        Assert.Equal(OperationsPaymentErrors.NotInFull, Assert.Throws<DomainException>(() => Pay(invoice, drawer, false, new OperationsTenderDraft(OperationsPaymentMethod.Cash, 30m, null, null))).Error);
        Assert.Equal(OperationsPaymentErrors.NotInFull, Assert.Throws<DomainException>(() => Pay(invoice, drawer, false, new OperationsTenderDraft(OperationsPaymentMethod.Cash, 45m, null, null))).Error);
        Assert.Equal(OperationsPaymentErrors.DrawerRequired, Assert.Throws<DomainException>(() => Pay(invoice, null, false, new OperationsTenderDraft(OperationsPaymentMethod.Cash, 40m, null, null))).Error);
        Assert.Equal(OperationsPaymentErrors.DrawerRequired, Assert.Throws<DomainException>(() =>
            Pay(invoice, OperationsCashDrawer.Open(Tenant, Warehouse, Guid.NewGuid(), 0m, Now), false, new OperationsTenderDraft(OperationsPaymentMethod.Cash, 40m, null, null))).Error);
        Assert.Equal(OperationsPaymentErrors.CashGivenTooLow, Assert.Throws<DomainException>(() => Pay(invoice, drawer, false, new OperationsTenderDraft(OperationsPaymentMethod.Cash, 40m, 20m, null))).Error);
        Assert.Equal(OperationsPaymentErrors.ZelleTermsRequired, Assert.Throws<DomainException>(() => Pay(invoice, drawer, false, new OperationsTenderDraft(OperationsPaymentMethod.Zelle, 40m, null, null))).Error);
        Assert.Equal(0m, invoice.PaidTotal);
    }

    [Fact]
    public void Zelle_WaitsForFinance_ReceivedIsPaid_NotReceivedIsOwedAgain()
    {
        OperationsInvoice invoice = Invoice();
        OperationsPayment payment = Pay(invoice, Drawer(), true, new OperationsTenderDraft(OperationsPaymentMethod.Cash, 15m, null, null),
            new OperationsTenderDraft(OperationsPaymentMethod.Zelle, 25m, null, "ZL123"));

        Assert.True(payment.Pending);
        Assert.Equal((OperationsPaymentState.AwaitingConfirmation, 0m, 15m, 25m), (invoice.PaymentState, invoice.Owed, invoice.PaidTotal, invoice.PendingTotal));
        OperationsPaymentTender zelle = payment.Tenders.Single(tender => tender.Method == OperationsPaymentMethod.Zelle);

        // Not received needs a reason; then the invoice owes the Zelle part again.
        Assert.Equal(OperationsPaymentErrors.ReasonRequired, Assert.Throws<DomainException>(() => payment.Check(invoice, zelle.Id, false, null, Cashier, Now)).Error);
        payment.Check(invoice, zelle.Id, false, "Nothing in the bank", Cashier, Now);
        Assert.Equal((OperationsPaymentState.Unpaid, 25m, false), (invoice.PaymentState, invoice.Owed, payment.Pending));
        Assert.Equal(OperationsPaymentErrors.NotAwaiting, Assert.Throws<DomainException>(() => payment.Check(invoice, zelle.Id, true, null, Cashier, Now)).Error);

        // Paid again by Zelle and received this time.
        OperationsPayment again = Pay(invoice, null, true, new OperationsTenderDraft(OperationsPaymentMethod.Zelle, 25m, null, null));
        again.Check(invoice, again.Tenders[0].Id, true, "BOA-778", Cashier, Now);
        Assert.Equal((OperationsPaymentState.Paid, 40m, "BOA-778"), (invoice.PaymentState, invoice.PaidTotal, again.Tenders[0].CheckNote));
    }

    [Fact]
    public void ADrawer_ClosesWithItsCount_AndADifferenceNeedsANote()
    {
        OperationsCashDrawer drawer = Drawer();
        Pay(Invoice(), drawer, false, new OperationsTenderDraft(OperationsPaymentMethod.Cash, 40m, 100m, null));

        Assert.Equal(OperationsPaymentErrors.ReasonRequired, Assert.Throws<DomainException>(() => drawer.Close(135m, null, Now)).Error);
        drawer.Close(135m, "Gave too much change", Now);
        Assert.Equal((OperationsCashDrawerStatus.Closed, -5m, 140m), (drawer.Status, drawer.Difference, drawer.Expected));
        Assert.Equal(OperationsPaymentErrors.DrawerClosed, Assert.Throws<DomainException>(() => drawer.Close(140m, null, Now)).Error);
        Assert.Equal(OperationsPaymentErrors.AmountInvalid, Assert.Throws<DomainException>(() => OperationsCashDrawer.Open(Tenant, Warehouse, Cashier, -1m, Now)).Error);
    }

    [Fact]
    public void ACreditNoteOnAPaidInvoice_IsGivenBack_NeverMoreThanCreditedOrPaid()
    {
        OperationsInvoice invoice = Invoice();
        OperationsCashDrawer drawer = Drawer();
        Pay(invoice, drawer, false, new OperationsTenderDraft(OperationsPaymentMethod.Cash, 40m, null, null));
        Assert.Equal(OperationsBillingErrors.VoidNotPossible, Assert.Throws<DomainException>(() =>
            OperationsCreditNote.RequestVoid(Tenant, invoice, "Wrong", Guid.NewGuid(), Guid.NewGuid(), Now)).Error);

        OperationsCreditNote credit = OperationsCreditNote.RequestCredit(Tenant, invoice, new Dictionary<Guid, decimal> { [invoice.Lines[0].Id] = 10m }, "Overcharged", Guid.NewGuid(), Guid.NewGuid(), Now);
        credit.Approve(invoice, "CN1", Guid.NewGuid(), DateOnly.MinValue, Now);
        Assert.Equal((-10m, 10m), (invoice.Owed, invoice.Refundable));

        Assert.Equal(OperationsPaymentErrors.RefundTooHigh, Assert.Throws<DomainException>(() =>
            OperationsRefund.Give(Tenant, invoice, "PAY2", OperationsPaymentMethod.Cash, 10.01m, "Overcharged", null, drawer, Cashier, Guid.NewGuid(), Now)).Error);
        Assert.Equal(OperationsPaymentErrors.ReasonRequired, Assert.Throws<DomainException>(() =>
            OperationsRefund.Give(Tenant, invoice, "PAY2", OperationsPaymentMethod.Cash, 10m, " ", null, drawer, Cashier, Guid.NewGuid(), Now)).Error);
        OperationsRefund.Give(Tenant, invoice, "PAY2", OperationsPaymentMethod.Cash, 10m, "Overcharged", null, drawer, Cashier, Guid.NewGuid(), Now);
        Assert.Equal((0m, 0m, 10m, 130m), (invoice.Owed, invoice.Refundable, drawer.CashOut, drawer.Expected));
    }
}
