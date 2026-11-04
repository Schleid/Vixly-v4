using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsInvoiceLine.cs
//
// PURPOSE:
// One line of an invoice as issued (Slice 5F, I-2 and I-3).
//
// RESPONSIBILITIES:
// - Keep what the line was: its charge kind or supply, its label, quantity, unit price, discount and amount
// - Keep its tax kind and whether it is taxed
// - Keep how much of it was credited
//
// IMPORTANT NOTES:
// - Kept as issued: a later price or name never changes it
// - Words are given on the page and the PDF from the charge kind; the label is the extra's, item's or supply's own name
// =========================================================
public sealed class OperationsInvoiceLine
{
    private OperationsInvoiceLine()
    {
    }

    public OperationsInvoiceLine(OperationsChargeLineKind? chargeKind, Guid? supplyId, string? code, string? label, int? piece, decimal quantity, decimal? basis, decimal unitPrice,
        decimal gross, decimal discountPercent, decimal discount, OperationsTaxKind taxKind, bool taxed)
    {
        Id = Guid.NewGuid();
        (ChargeKind, SupplyId, Code, Label, Piece, Quantity, Basis, UnitPrice) = (chargeKind, supplyId, code, label, piece, quantity, basis, unitPrice);
        (Gross, DiscountPercent, Discount, TaxKind, Taxed) = (OperationsBillingLimits.Money(gross), discountPercent, OperationsBillingLimits.Money(discount), taxKind, taxed);
    }

    public Guid Id { get; private set; }

    // A charge of the shipment; null for a supply.
    public OperationsChargeLineKind? ChargeKind { get; private set; }

    public Guid? SupplyId { get; private set; }

    public string? Code { get; private set; }

    public string? Label { get; private set; }

    public int? Piece { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal? Basis { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal Gross { get; private set; }

    public decimal DiscountPercent { get; private set; }

    public decimal Discount { get; private set; }

    public decimal Amount => Gross - Discount;

    public OperationsTaxKind TaxKind { get; private set; }

    public bool Taxed { get; private set; }

    // What approved credit notes took back from this line, before tax.
    public decimal Credited { get; private set; }

    public decimal Remaining => Amount - Credited;

    internal void Credit(decimal amount) => Credited += amount;
}
