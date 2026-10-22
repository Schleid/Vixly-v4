using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsBillingDomainTests.cs
//
// PURPOSE:
// Proves the invoice rules (Slice 5F step 1, I-3 to I-7).
//
// RESPONSIBILITIES:
// - Taxes per kind on taxed lines, rounded to the cent
// - Credit notes: never more than a line holds, taxes in proportion and to the cent at the end, decided by another person
// - Voids only while nothing is paid or credited
// - The invoice settings
//
// IMPORTANT NOTES:
// - Pure domain; no database
// =========================================================
public sealed class OperationsBillingDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Clerk = Guid.NewGuid();

    private static readonly Guid Finance = Guid.NewGuid();

    // Freight 20.00 (taxed at 0), insurance 1.50, a government fee 5.00 (never taxed), two taped boxes 9.00 with 10% off (taxed 7%), a bag 1.00 not taxed.
    private static OperationsInvoice Invoice(bool perKind = false)
    {
        OperationsInvoiceLine[] lines =
        {
            new(OperationsChargeLineKind.Freight, null, null, null, 1, 1m, 4m, 5m, 20.00m, 0m, 0m, OperationsTaxKind.Freight, true),
            new(OperationsChargeLineKind.Insurance, null, null, null, null, 1m, null, 1.50m, 1.50m, 0m, 0m, OperationsTaxKind.Insurance, true),
            new(OperationsChargeLineKind.Extra, null, null, "Customs fee", null, 1m, null, 5m, 5.00m, 0m, 0m, OperationsTaxKind.Charges, false),
            new(null, Guid.NewGuid(), "TAPE", "Packing tape", null, 2m, null, 4.50m, 9.00m, 10m, 0.90m, OperationsTaxKind.Supplies, true),
            new(null, Guid.NewGuid(), "BAG", "Bag", null, 1m, null, 1.00m, 1.00m, 0m, 0m, OperationsTaxKind.Supplies, false)
        };
        OperationsInvoiceDraft draft = new(OperationsInvoiceKind.Shipment, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "SH26000001", "Miami → Port-au-Prince", "air_standard",
            "USD", "fr", new OperationsInvoiceParty("Galaxy Freight", "TX-1", "Miami", "C100", "Ana Pierre", null, "+13055550100", "ana@example.com"), lines, "Sales tax", perKind,
            new OperationsTaxRates(0m, 6m, 7m, 7m), "Thank you");
        return OperationsInvoice.Issue(Tenant, "INV26000001", draft, DateOnly.FromDateTime(Now.UtcDateTime), Now);
    }

    [Fact]
    public void Taxes_ArePerKindOnTaxedLines_RoundedToTheCent_AndTheTotalsAreFixed()
    {
        OperationsInvoice invoice = Invoice();

        // Insurance 1.50 × 6% = 0.09; supplies 8.10 × 7% = 0.567 → 0.57; the fee and the bag are not taxed; freight has no rate.
        Assert.Equal(new[] { (OperationsTaxKind.Insurance, 1.50m, 0.09m), (OperationsTaxKind.Supplies, 8.10m, 0.57m) },
            invoice.Taxes.Select(tax => (tax.Kind, tax.Basis, tax.Amount)));
        Assert.Equal((36.50m, 0.90m, 35.60m, 0.66m, 36.26m), (invoice.Gross, invoice.DiscountTotal, invoice.Subtotal, invoice.TaxTotal, invoice.Total));
        Assert.Equal(36.26m, invoice.Owed);
        Assert.True(invoice.CanVoid);
        Assert.Equal(("fr", "Galaxy Freight", "Thank you"), (invoice.Language, invoice.Party.CompanyName, invoice.Note));
    }

    [Fact]
    public void NothingToInvoice_IsRefused()
    {
        OperationsInvoiceDraft draft = new(OperationsInvoiceKind.SupplyOrder, Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), "SO26000001", null, null, "USD", "en",
            new OperationsInvoiceParty("Galaxy Freight", null, "Miami", null, "Ana Pierre", null, "+1", "a@example.com"), Array.Empty<OperationsInvoiceLine>(), null, false,
            OperationsTaxRates.None, null);

        Assert.Equal(OperationsBillingErrors.NothingToInvoice, Assert.Throws<DomainException>(() => OperationsInvoice.Issue(Tenant, "INV1", draft, DateOnly.MinValue, Now)).Error);
    }

    [Fact]
    public void ACreditNote_TakesBackPartOfALine_WithItsTaxInProportion_AndTheLastTakesWhatIsLeft()
    {
        OperationsInvoice invoice = Invoice();
        OperationsInvoiceLine tape = invoice.Lines.Single(line => line.Code == "TAPE");

        // Half of the tape (4.05 of 8.10): tax 0.57 × 4.05 / 8.10 = 0.285 → 0.29.
        OperationsCreditNote first = OperationsCreditNote.RequestCredit(Tenant, invoice, new Dictionary<Guid, decimal> { [tape.Id] = 4.05m }, "Damaged", Clerk, Guid.NewGuid(), Now);
        Assert.Equal((4.05m, 0.29m, 4.34m), (first.Subtotal, first.TaxTotal, first.Total));
        Assert.Equal(OperationsCreditNoteStatus.Requested, first.Status);
        Assert.Equal(36.26m, invoice.Owed);

        // The one who asked never decides; another person approves and the invoice takes it back.
        Assert.Equal(OperationsBillingErrors.OwnRequest, Assert.Throws<DomainException>(() => first.Approve(invoice, "CN1", Clerk, DateOnly.FromDateTime(Now.UtcDateTime), Now)).Error);
        first.Approve(invoice, "CN26000001", Finance, DateOnly.FromDateTime(Now.UtcDateTime), Now);
        Assert.Equal(("CN26000001", 4.05m, 4.34m, 31.92m), (first.Number, tape.Credited, invoice.CreditedTotal, invoice.Owed));
        Assert.Equal(OperationsBillingErrors.NotRequested, Assert.Throws<DomainException>(() => first.Refuse(null, Finance, Now)).Error);

        // More than the line still holds is refused; the rest of it takes the tax left to the cent (0.28, not 0.285 rounded).
        Assert.Equal(OperationsBillingErrors.AmountInvalid, Assert.Throws<DomainException>(() =>
            OperationsCreditNote.RequestCredit(Tenant, invoice, new Dictionary<Guid, decimal> { [tape.Id] = 4.06m }, "Damaged", Clerk, Guid.NewGuid(), Now)).Error);
        OperationsCreditNote second = OperationsCreditNote.RequestCredit(Tenant, invoice, new Dictionary<Guid, decimal> { [tape.Id] = 4.05m }, "Damaged", Clerk, Guid.NewGuid(), Now);
        Assert.Equal(0.28m, second.TaxTotal);
        second.Approve(invoice, "CN26000002", Finance, DateOnly.FromDateTime(Now.UtcDateTime), Now);
        Assert.Equal(0.57m, invoice.Taxes.Single(tax => tax.Kind == OperationsTaxKind.Supplies).Credited);

        // Once credited, it cannot be voided: the rest goes by credit notes.
        Assert.False(invoice.CanVoid);
        Assert.Equal(OperationsBillingErrors.VoidNotPossible, Assert.Throws<DomainException>(() =>
            OperationsCreditNote.RequestVoid(Tenant, invoice, "Wrong customer", Clerk, Guid.NewGuid(), Now)).Error);
    }

    // Review of 5F: a request made before another was approved has its taxes worked out again when it is approved.
    [Fact]
    public void ARequestMadeEarlier_TakesItsTaxFromTheInvoiceAsItIsWhenApproved()
    {
        OperationsInvoice invoice = Invoice();
        OperationsInvoiceLine tape = invoice.Lines.Single(line => line.Code == "TAPE");
        OperationsCreditNote first = OperationsCreditNote.RequestCredit(Tenant, invoice, new Dictionary<Guid, decimal> { [tape.Id] = 4.05m }, "Damaged", Clerk, Guid.NewGuid(), Now);
        OperationsCreditNote second = OperationsCreditNote.RequestCredit(Tenant, invoice, new Dictionary<Guid, decimal> { [tape.Id] = 4.05m }, "Damaged", Clerk, Guid.NewGuid(), Now);
        Assert.Equal(0.29m, second.TaxTotal);

        first.Approve(invoice, "CN1", Finance, DateOnly.FromDateTime(Now.UtcDateTime), Now);
        second.Approve(invoice, "CN2", Finance, DateOnly.FromDateTime(Now.UtcDateTime), Now);
        Assert.Equal((0.28m, 4.33m), (second.TaxTotal, second.Total));
        Assert.Equal(0.57m, invoice.Taxes.Single(tax => tax.Kind == OperationsTaxKind.Supplies).Credited);
        Assert.Equal(DateOnly.FromDateTime(Now.UtcDateTime), second.DecidedOn);
    }

    [Fact]
    public void ARequest_NeedsAReasonAndAnAmount_AndARefusalChangesNothing()
    {
        OperationsInvoice invoice = Invoice();
        Guid freight = invoice.Lines[0].Id;

        Assert.Equal(OperationsBillingErrors.ReasonRequired, Assert.Throws<DomainException>(() =>
            OperationsCreditNote.RequestCredit(Tenant, invoice, new Dictionary<Guid, decimal> { [freight] = 1m }, "  ", Clerk, Guid.NewGuid(), Now)).Error);
        Assert.Equal(OperationsBillingErrors.NothingToCredit, Assert.Throws<DomainException>(() =>
            OperationsCreditNote.RequestCredit(Tenant, invoice, new Dictionary<Guid, decimal> { [freight] = 0m }, "Overcharged", Clerk, Guid.NewGuid(), Now)).Error);
        Assert.Equal(OperationsBillingErrors.AmountInvalid, Assert.Throws<DomainException>(() =>
            OperationsCreditNote.RequestCredit(Tenant, invoice, new Dictionary<Guid, decimal> { [freight] = 1.005m }, "Overcharged", Clerk, Guid.NewGuid(), Now)).Error);
        Assert.Equal(OperationsBillingErrors.AmountInvalid, Assert.Throws<DomainException>(() =>
            OperationsCreditNote.RequestCredit(Tenant, invoice, new Dictionary<Guid, decimal> { [Guid.NewGuid()] = 1m }, "Overcharged", Clerk, Guid.NewGuid(), Now)).Error);

        // Freight has no tax: a credit of it gives back no tax.
        OperationsCreditNote note = OperationsCreditNote.RequestCredit(Tenant, invoice, new Dictionary<Guid, decimal> { [freight] = 2m }, "Overcharged", Clerk, Guid.NewGuid(), Now);
        Assert.Empty(note.Taxes);
        note.Refuse("Price was right", Finance, Now);
        Assert.Equal((OperationsCreditNoteStatus.Refused, "Price was right", 0m, 36.26m), (note.Status, note.RefusalNote, invoice.CreditedTotal, invoice.Owed));
    }

    [Fact]
    public void AVoid_TakesTheWholeInvoice_AndNothingIsOwed()
    {
        OperationsInvoice invoice = Invoice(perKind: true);
        OperationsCreditNote voiding = OperationsCreditNote.RequestVoid(Tenant, invoice, "Wrong customer", Clerk, Guid.NewGuid(), Now);
        Assert.Equal((OperationsCreditNoteKind.Void, 36.26m), (voiding.Kind, voiding.Total));

        voiding.Approve(invoice, "CN26000003", Finance, DateOnly.FromDateTime(Now.UtcDateTime), Now);
        Assert.Equal((OperationsInvoiceStatus.Voided, 0m), (invoice.Status, invoice.Owed));
        Assert.NotNull(invoice.VoidedUtc);
        Assert.Equal(OperationsBillingErrors.Voided, Assert.Throws<DomainException>(() =>
            OperationsCreditNote.RequestCredit(Tenant, invoice, new Dictionary<Guid, decimal> { [invoice.Lines[0].Id] = 1m }, "Late", Clerk, Guid.NewGuid(), Now)).Error);
    }

    [Fact]
    public void InvoiceSettings_CheckTheRates_KeepTheNotePerLanguage_AndTheCreditNotePrefixDiffers()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);
        Assert.Equal(("CN", 0m, false), (settings.CreditNotePrefix, settings.TaxRates.Supplies, settings.TaxPerKind));

        settings.ChangeInvoicing(" Sales tax ", "TX-1", new OperationsTaxRates(0m, 0m, 7.125m, 7m), true, "Pay within 7 days", null, "Pague en 7 días", Now);
        Assert.Equal(("Sales tax", 7.125m, true), (settings.TaxName, settings.SuppliesTaxPercent, settings.TaxPerKind));
        Assert.Equal(("Pay within 7 days", "Pague en 7 días"), (settings.InvoiceNote("fr"), settings.InvoiceNote("es")));
        Assert.Equal(OperationsBillingErrors.TaxRateInvalid, Assert.Throws<DomainException>(() =>
            settings.ChangeInvoicing(null, null, new OperationsTaxRates(0m, 0m, 100.5m, 0m), false, null, null, null, Now)).Error);
        Assert.Equal(OperationsBillingErrors.TaxRateInvalid, Assert.Throws<DomainException>(() =>
            settings.ChangeInvoicing(null, null, new OperationsTaxRates(0m, 0m, 7.1234m, 0m), false, null, null, null, Now)).Error);

        Assert.Equal(OperationsSettingsErrors.PrefixesMustDiffer, Assert.Throws<DomainException>(() =>
            settings.ChangePrefixes("SH", "RC", "INV", "PAY", "C", "PK", "SO", "INV", Now)).Error);
        settings.ChangePrefixes("SH", "RC", "INV", "PAY", "C", "PK", "SO", "AV", Now);
        Assert.Equal("AV", settings.Prefix(OperationsNumberKind.CreditNote));
    }
}
