using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformInvoiceLine.cs
//
// PURPOSE:
// One charged line of an invoice, with its tax.
//
// RESPONSIBILITIES:
// - Hold the kind, translated text key and arguments, amounts and the tax applied
//
// IMPORTANT NOTES:
// - Owned by PlatformInvoice; never changed after issue
// - Net and tax are rounded to the cent half away from zero
// =========================================================
public sealed class PlatformInvoiceLine : AuditableEntity
{
    private PlatformInvoiceLine()
    {
        TextKey = string.Empty;
        Arguments = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    internal PlatformInvoiceLine(Guid invoiceId, int position, PlatformInvoiceLineInput input, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformInvoiceId = invoiceId;
        Position = position;
        Kind = input.Kind;
        TextKey = input.TextKey;
        Arguments = input.Arguments;
        Quantity = input.Quantity;
        UnitPrice = input.UnitPrice;
        Net = PlatformBillingLimits.Money(input.Quantity * input.UnitPrice);
        TaxRate = input.TaxRate;
        TaxLabel = input.TaxLabel;
        Tax = PlatformBillingLimits.Money(Net * input.TaxRate / 100m);
        SourceId = input.SourceId;
    }

    public Guid PlatformInvoiceId { get; private set; }

    public int Position { get; private set; }

    public PlatformInvoiceLineKind Kind { get; private set; }

    public string TextKey { get; private set; }

    public IReadOnlyDictionary<string, string> Arguments { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal Net { get; private set; }

    public decimal TaxRate { get; private set; }

    public string? TaxLabel { get; private set; }

    public decimal Tax { get; private set; }

    public Guid? SourceId { get; private set; }

    internal static void Validate(PlatformInvoiceLineInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        bool discount = input.Kind == PlatformInvoiceLineKind.Discount;
        bool priceOk = discount ? input.UnitPrice < 0m : input.UnitPrice > 0m;

        if (!Enum.IsDefined(input.Kind)
            || string.IsNullOrWhiteSpace(input.TextKey) || input.TextKey.Length > PlatformBillingLimits.TextKeyMaxLength
            || input.Quantity <= 0m || input.Quantity > PlatformBillingLimits.MaxAmount || decimal.Round(input.Quantity, 2) != input.Quantity
            || !priceOk || Math.Abs(input.UnitPrice) > PlatformBillingLimits.MaxAmount || !PlatformBillingLimits.IsMoney(input.UnitPrice)
            || input.TaxRate < 0m || input.TaxRate > PlatformBillingLimits.MaxRate || decimal.Round(input.TaxRate, PlatformBillingLimits.RateScale) != input.TaxRate
            || input.TaxLabel?.Length > PlatformBillingLimits.TaxLabelMaxLength)
        {
            throw new DomainException(PlatformBillingErrors.LineInvalid);
        }
    }
}
