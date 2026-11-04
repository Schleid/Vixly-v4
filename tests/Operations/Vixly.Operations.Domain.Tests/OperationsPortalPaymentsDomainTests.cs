using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsPortalPaymentsDomainTests.cs
//
// PURPOSE:
// Proves paying on the customer portal (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - A method shows only when switched on, accepted and set up; cash and cheque never
// - Zelle and bank transfer are reported for the whole amount, with a reference, and wait for finance
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsPortalPaymentsDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly Today = new(2026, 10, 27);

    private static OperationsInvoice Invoice(decimal amount)
    {
        OperationsInvoiceLine[] lines = { new(OperationsChargeLineKind.Freight, null, null, null, 1, 1m, 10m, 4m, amount, 0m, 0m, OperationsTaxKind.Freight, true) };
        return OperationsInvoice.Issue(Tenant, "INV" + amount, new OperationsInvoiceDraft(OperationsInvoiceKind.Shipment, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "SH1", null, null,
            "USD", "en", new OperationsInvoiceParty("Galaxy Freight", null, "Miami", "C1", "Ana", null, "+1", "a@example.com"), lines, null, false, OperationsTaxRates.None, null), Today, Now);
    }

    private static OperationsTenantSettings Accepting()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);
        settings.ChangeZelle("@andes", null, "Andes Courier", "Send the invoice number in the memo.", null, null, Now);
        settings.ChangePayments(true, true, true, true, null, null, null, Now);
        settings.ChangeCard(true, Now);
        return settings;
    }

    [Fact]
    public void Methods_ShowOnlyWhenSwitchedOn_AcceptedAndSetUp()
    {
        OperationsTenantSettings settings = Accepting();
        Assert.False(settings.OffersOnPortal(OperationsPaymentMethod.Zelle, true));

        settings.ChangePortal(true, true, true, "Bank of Haiti, account 123", null, null, Now);
        Assert.True(settings.OffersOnPortal(OperationsPaymentMethod.Zelle, false));
        Assert.True(settings.OffersOnPortal(OperationsPaymentMethod.BankTransfer, false));
        Assert.False(settings.OffersOnPortal(OperationsPaymentMethod.Card, false));
        Assert.True(settings.OffersOnPortal(OperationsPaymentMethod.Card, true));
        Assert.False(settings.OffersOnPortal(OperationsPaymentMethod.Cash, true));
        Assert.False(settings.OffersOnPortal(OperationsPaymentMethod.Cheque, true));
        Assert.Equal("Bank of Haiti, account 123", settings.BankTransferInstructions("fr"));

        // Not accepted any more: not offered, even switched on.
        settings.ChangePayments(true, false, false, false, null, null, null, Now);
        Assert.False(settings.OffersOnPortal(OperationsPaymentMethod.Zelle, false));
        Assert.False(settings.OffersOnPortal(OperationsPaymentMethod.BankTransfer, false));
    }

    [Fact]
    public void SwitchingOn_NeedsWhatCustomersAreTold()
    {
        OperationsTenantSettings settings = Accepting();
        Assert.Equal(OperationsPaymentErrors.BankInstructionsRequired,
            Assert.Throws<DomainException>(() => settings.ChangePortal(false, true, false, " ", null, null, Now)).Error);

        settings.ChangeZelle("@andes", null, null, "Terms", null, null, Now);
        Assert.Equal(OperationsPaymentErrors.ZelleNameRequired, Assert.Throws<DomainException>(() => settings.ChangePortal(true, false, false, null, null, null, Now)).Error);
    }

    [Fact]
    public void Report_IsTheWholeAmount_WithAReference_AndWaitsForFinance()
    {
        OperationsInvoice invoice = Invoice(20m);

        Assert.Equal(OperationsPaymentErrors.ZelleTermsRequired,
            Assert.Throws<DomainException>(() => OperationsPayment.Report(Tenant, invoice, "PAY1", OperationsPaymentMethod.Zelle, "ZL-1", false, Guid.NewGuid(), Now)).Error);
        Assert.Equal(OperationsPaymentErrors.ReferenceRequired,
            Assert.Throws<DomainException>(() => OperationsPayment.Report(Tenant, invoice, "PAY1", OperationsPaymentMethod.BankTransfer, "  ", false, Guid.NewGuid(), Now)).Error);
        Assert.Equal(OperationsPaymentErrors.MethodNotAccepted,
            Assert.Throws<DomainException>(() => OperationsPayment.Report(Tenant, invoice, "PAY1", OperationsPaymentMethod.Cash, "x", true, Guid.NewGuid(), Now)).Error);

        OperationsPayment payment = OperationsPayment.Report(Tenant, invoice, "PAY1", OperationsPaymentMethod.BankTransfer, "WIRE 778", false, Guid.NewGuid(), Now);

        Assert.True(payment.Pending);
        Assert.Null(payment.TakenByUserId);
        Assert.Equal(20m, payment.Total);
        Assert.Equal(OperationsTenderStatus.AwaitingConfirmation, Assert.Single(payment.Tenders).Status);
        Assert.Equal("WIRE 778", payment.Tenders[0].Reference);
        Assert.Equal(0m, invoice.Owed);
        Assert.Equal(20m, invoice.PendingTotal);

        // Nothing left: a second report is refused.
        Assert.Equal(OperationsPaymentErrors.NothingOwed,
            Assert.Throws<DomainException>(() => OperationsPayment.Report(Tenant, invoice, "PAY2", OperationsPaymentMethod.Zelle, "ZL-2", true, Guid.NewGuid(), Now)).Error);
    }
}
