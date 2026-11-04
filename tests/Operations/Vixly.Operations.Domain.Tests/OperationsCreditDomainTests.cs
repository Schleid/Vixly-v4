using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsCreditDomainTests.cs
//
// PURPOSE:
// Proves the credit account rules (Slice 5F step 2b, K-1 to K-5).
//
// RESPONSIBILITIES:
// - An account's limit, terms and state; closed only when nothing is owed
// - A charge releases the invoice and keeps it owed; above the limit it waits for another person
// - Where an account stands: owed, available, overdue, by age
//
// IMPORTANT NOTES:
// - Pure domain; no database
// =========================================================
public sealed class OperationsCreditDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly Guid Cashier = Guid.NewGuid();

    private static readonly Guid Credit = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly Today = new(2026, 10, 21);

    private static OperationsInvoice Invoice(decimal amount, Guid customer)
    {
        OperationsInvoiceLine[] lines = { new(OperationsChargeLineKind.Freight, null, null, null, 1, 1m, 10m, 4m, amount, 0m, 0m, OperationsTaxKind.Freight, true) };
        return OperationsInvoice.Issue(Tenant, "INV" + amount, new OperationsInvoiceDraft(OperationsInvoiceKind.Shipment, customer, Guid.NewGuid(), Guid.NewGuid(), null, "SH1", null, null, "USD", "en",
            new OperationsInvoiceParty("Galaxy Freight", null, "Miami", "C1", "Ana", null, "+1", "a@example.com"), lines, null, false, OperationsTaxRates.None, null), Today, Now);
    }

    [Fact]
    public void AnAccount_HasALimitTermsAndAState_AndClosesOnlyWhenNothingIsOwed()
    {
        Assert.Equal(OperationsCreditErrors.LimitInvalid, Assert.Throws<DomainException>(() => OperationsCreditAccount.Open(Tenant, Guid.NewGuid(), -1m, null, Credit, Now)).Error);
        Assert.Equal(OperationsCreditErrors.LimitInvalid, Assert.Throws<DomainException>(() => OperationsCreditAccount.Open(Tenant, Guid.NewGuid(), 10.001m, null, Credit, Now)).Error);
        Assert.Equal(OperationsCreditErrors.TermsInvalid, Assert.Throws<DomainException>(() => OperationsCreditAccount.Open(Tenant, Guid.NewGuid(), 100m, 400, Credit, Now)).Error);

        OperationsCreditAccount account = OperationsCreditAccount.Open(Tenant, Guid.NewGuid(), 500m, null, Credit, Now);
        Assert.Equal((OperationsCreditAccountStatus.Active, 30, 15), (account.Status, account.TermsOr(30), OperationsCreditAccount.Open(Tenant, Guid.NewGuid(), 1m, 15, Credit, Now).TermsOr(30)));

        Assert.Equal(OperationsCreditErrors.ReasonRequired, Assert.Throws<DomainException>(() => account.Change(500m, null, OperationsCreditAccountStatus.OnHold, " ", 0m, Credit, Now)).Error);
        account.Change(800m, 45, OperationsCreditAccountStatus.OnHold, "Two invoices overdue", 0m, Credit, Now);
        Assert.Equal((800m, 45, OperationsCreditAccountStatus.OnHold, "Two invoices overdue", false), (account.Limit, account.TermsDays!.Value, account.Status, account.Reason, account.IsActive));

        Assert.Equal(OperationsCreditErrors.BalanceOpen, Assert.Throws<DomainException>(() => account.Change(800m, 45, OperationsCreditAccountStatus.Closed, "Moved away", 12m, Credit, Now)).Error);
        account.Change(800m, 45, OperationsCreditAccountStatus.Active, null, 12m, Credit, Now);
        Assert.Equal((OperationsCreditAccountStatus.Active, (string?)null), (account.Status, account.Reason));
        account.Change(0m, null, OperationsCreditAccountStatus.Closed, "Moved away", 0m, Credit, Now);
        Assert.Equal(OperationsCreditAccountStatus.Closed, account.Status);
    }

    [Fact]
    public void ACharge_ReleasesTheInvoice_AndKeepsItOwed_UntilPaid()
    {
        OperationsInvoice invoice = Invoice(40m, Guid.NewGuid());
        Assert.False(invoice.Released);

        invoice.ChargeToAccount(true, Today.AddDays(30), Cashier, Now);
        Assert.Equal((OperationsPaymentState.OnAccount, true, 40m, Today.AddDays(30)), (invoice.PaymentState, invoice.Released, invoice.Owed, invoice.DueOn!.Value));
        Assert.Equal(OperationsCreditErrors.AlreadyOnAccount, Assert.Throws<DomainException>(() => invoice.ChargeToAccount(true, Today, Cashier, Now)).Error);

        // Paid at the cashier later: off the account, paid.
        OperationsCashDrawer drawer = OperationsCashDrawer.Open(Tenant, invoice.WarehouseId, Cashier, 0m, Now);
        OperationsPayment.Take(Tenant, invoice, "PAY1", new[] { new OperationsTenderDraft(OperationsPaymentMethod.Cash, 40m, null, null) }, drawer, false, Cashier, Guid.NewGuid(), Now);
        Assert.Equal((OperationsPaymentState.Paid, true, 0m), (invoice.PaymentState, invoice.Released, invoice.Owed));

        // Paid in full by Zelle: while finance checks it, the shipment stays released (review).
        OperationsInvoice zelle = Invoice(30m, Guid.NewGuid());
        zelle.ChargeToAccount(true, Today.AddDays(30), Cashier, Now);
        OperationsPayment.Take(Tenant, zelle, "PAY2", new[] { new OperationsTenderDraft(OperationsPaymentMethod.Zelle, 30m, null, "ZL1") }, null, true, Cashier, Guid.NewGuid(), Now);
        Assert.Equal((OperationsPaymentState.OnAccount, true, 0m), (zelle.PaymentState, zelle.Released, zelle.Owed));
    }

    [Fact]
    public void AboveTheLimit_TheChargeWaits_ForAnotherPersonOfTheCreditDepartment()
    {
        OperationsInvoice invoice = Invoice(40m, Guid.NewGuid());
        invoice.ChargeToAccount(false, Today.AddDays(30), Cashier, Now);
        Assert.Equal((OperationsPaymentState.AwaitingCredit, false, (DateOnly?)null), (invoice.PaymentState, invoice.Released, invoice.DueOn));

        Assert.Equal(OperationsCreditErrors.OwnRequest, Assert.Throws<DomainException>(() => invoice.DecideCharge(true, Today.AddDays(30), Cashier, Now)).Error);
        invoice.DecideCharge(true, Today.AddDays(30), Credit, Now);
        Assert.Equal((OperationsPaymentState.OnAccount, Credit), (invoice.PaymentState, invoice.AccountDecidedByUserId!.Value));
        Assert.Equal(OperationsCreditErrors.NotAwaiting, Assert.Throws<DomainException>(() => invoice.DecideCharge(false, Today, Credit, Now)).Error);

        // Paid at the cashier while waiting: nothing left to decide (review).
        OperationsInvoice paid = Invoice(20m, Guid.NewGuid());
        paid.ChargeToAccount(false, Today, Cashier, Now);
        OperationsPayment.Take(Tenant, paid, "PAY3", new[] { new OperationsTenderDraft(OperationsPaymentMethod.Cash, 20m, null, null) },
            OperationsCashDrawer.Open(Tenant, paid.WarehouseId, Cashier, 0m, Now), false, Cashier, Guid.NewGuid(), Now);
        Assert.Equal(OperationsCreditErrors.NothingOwed, Assert.Throws<DomainException>(() => paid.DecideCharge(true, Today, Credit, Now)).Error);

        OperationsInvoice refused = Invoice(10m, Guid.NewGuid());
        refused.ChargeToAccount(false, Today, Cashier, Now);
        refused.DecideCharge(false, Today, Credit, Now);
        Assert.Equal((OperationsPaymentState.Unpaid, OperationsAccountCharge.None), (refused.PaymentState, refused.OnAccount));
    }

    [Fact]
    public void AnAccount_StandsByItsOpenItems_OwedAvailableOverdueAndByAge()
    {
        Guid customer = Guid.NewGuid();
        OperationsCreditAccount account = OperationsCreditAccount.Open(Tenant, customer, 200m, null, Credit, Now);
        OperationsInvoice current = Charged(Invoice(50m, customer), Today.AddDays(10));
        OperationsInvoice late = Charged(Invoice(30m, customer), Today.AddDays(-45));
        OperationsInvoice veryLate = Charged(Invoice(20m, customer), Today.AddDays(-100));
        OperationsInvoice waiting = Invoice(25m, customer);
        waiting.ChargeToAccount(false, Today, Cashier, Now);

        OperationsCreditPosition position = OperationsCreditPosition.Of(account, new[] { current, late, veryLate, waiting }, Today);
        Assert.Equal((100m, 25m, 75m, 50m), (position.Owed, position.Awaiting, position.Available, position.Overdue));
        Assert.Equal(new[] { 50m, 0m, 30m, 0m, 20m }, position.Ages);

        Assert.Equal(0m, OperationsCreditPosition.Of(OperationsCreditAccount.Open(Tenant, customer, 50m, null, Credit, Now), new[] { current, late, veryLate }, Today).Available);
    }

    private static OperationsInvoice Charged(OperationsInvoice invoice, DateOnly due)
    {
        invoice.ChargeToAccount(true, due, Cashier, Now);
        return invoice;
    }

    [Fact]
    public void TheCompany_SetsItsTerms_WhatAChargeAboveTheLimitDoes_AndTheMonthlyStatement()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);
        Assert.Equal((30, OperationsCreditOverLimit.Refuse, true), (settings.CreditTermsDays, settings.CreditOverLimit, settings.CreditStatements));

        settings.ChangeCredit(15, OperationsCreditOverLimit.AskCreditDepartment, false, Now);
        Assert.Equal((15, OperationsCreditOverLimit.AskCreditDepartment, false), (settings.CreditTermsDays, settings.CreditOverLimit, settings.CreditStatements));
        Assert.Equal(OperationsCreditErrors.SettingsInvalid, Assert.Throws<DomainException>(() => settings.ChangeCredit(-1, OperationsCreditOverLimit.Refuse, true, Now)).Error);
    }
}
