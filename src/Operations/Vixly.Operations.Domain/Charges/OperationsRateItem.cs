namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsRateItem.cs
//
// PURPOSE:
// A fixed price for one of the company's item types, whatever it weighs: a barrel, a car (owner, Slice 4).
//
// RESPONSIBILITIES:
// - Keep the item type and its price
//
// IMPORTANT NOTES:
// - Stored inside the price version (JSON); item types come from the company's list (Slice 4B section 7)
// =========================================================
public sealed class OperationsRateItem
{
    private OperationsRateItem()
    {
    }

    public OperationsRateItem(Guid itemTypeId, decimal price)
    {
        (ItemTypeId, Price) = (itemTypeId, price);
    }

    public Guid ItemTypeId { get; private set; }

    public decimal Price { get; private set; }
}
