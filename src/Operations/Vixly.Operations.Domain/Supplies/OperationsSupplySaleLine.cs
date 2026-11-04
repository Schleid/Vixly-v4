using Vixly.Operations.Domain.Charges;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplySaleLine.cs
//
// PURPOSE:
// One supply on a sale (Slice 5E, S-11, S-12 and S-19): how many, at which price, with any discount and who gave and approved it.
//
// RESPONSIBILITIES:
// - Keep the supply, its code and name as sold, the quantity and the price version
// - Keep the discount, its reason, who gave it and who approved it
// - Give the amount
//
// IMPORTANT NOTES:
// - The price version is kept: a later price change never changes this line (S-12); its quantity breaks still apply
// - A discount is always below 100% (no free items, owner)
// =========================================================
public sealed class OperationsSupplySaleLine
{
    private OperationsSupplySaleLine()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    internal OperationsSupplySaleLine(Guid supplyId, string code, string name, bool taxed, Guid priceId)
    {
        (Id, SupplyId, Code, Name, Taxed, PriceId) = (Guid.NewGuid(), supplyId, code, name, taxed, priceId);
    }

    public Guid Id { get; private set; }

    public Guid SupplyId { get; private set; }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public bool Taxed { get; private set; }

    public Guid PriceId { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal DiscountPercent { get; private set; }

    public string? DiscountReason { get; private set; }

    public Guid? DiscountByUserId { get; private set; }

    public Guid? DiscountApprovedByUserId { get; private set; }

    public decimal Gross => decimal.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);

    public decimal Discount => decimal.Round(Gross * DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);

    public decimal Amount => Gross - Discount;

    internal void Count(int quantity, decimal unitPrice)
    {
        Quantity = quantity is > 0 and <= OperationsSupplyLimits.MaxQuantity ? quantity : throw new DomainException(OperationsSupplyStockErrors.QuantityInvalid);
        UnitPrice = OperationsChargeLimits.Money(unitPrice, OperationsSupplyErrors.PriceInvalid);
    }

    internal void Discounted(decimal percent, string? reason, Guid? byUserId, Guid? approvedByUserId)
    {
        if (percent is < 0m or >= 100m || decimal.Round(percent, 2) != percent)
        {
            throw new DomainException(OperationsSupplySaleErrors.DiscountInvalid);
        }

        DiscountPercent = percent;
        (DiscountReason, DiscountByUserId, DiscountApprovedByUserId) = percent == 0m
            ? (null, null, null)
            : (OperationsChargeLimits.Text(reason, OperationsSupplyStockLimits.NoteMaxLength, OperationsSupplyErrors.TextInvalid), byUserId, approvedByUserId);
    }
}
