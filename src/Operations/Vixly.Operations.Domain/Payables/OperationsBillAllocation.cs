namespace Vixly.Operations.Domain.Payables;

// =========================================================
// FILE: OperationsBillAllocation.cs
//
// PURPOSE:
// A share of a bill put on a dispatch or on a shipment (Slice 8): the cost that shipment’s profit carries.
//
// RESPONSIBILITIES:
// - Keep the dispatch or the shipment, its number as entered, and the amount
//
// IMPORTANT NOTES:
// - Owned by its bill; a carrier’s share on a dispatch is spread on the dispatch’s shipments by chargeable weight
// =========================================================
public sealed class OperationsBillAllocation
{
    private OperationsBillAllocation()
    {
        Reference = string.Empty;
    }

    public OperationsBillAllocation(Guid? dispatchId, Guid? shipmentId, string reference, decimal amount)
    {
        if ((dispatchId is null) == (shipmentId is null) || string.IsNullOrWhiteSpace(reference))
        {
            throw new ArgumentException("A dispatch or a shipment, with its number.", nameof(dispatchId));
        }

        (Id, DispatchId, ShipmentId, Reference, Amount) = (Guid.NewGuid(), dispatchId, shipmentId, reference, OperationsPayableText.Money(amount, OperationsPayableErrors.AllocationInvalid));
    }

    public Guid Id { get; private set; }

    public Guid? DispatchId { get; private set; }

    public Guid? ShipmentId { get; private set; }

    // The dispatch or shipment number, as it read when the bill was entered.
    public string Reference { get; private set; }

    public decimal Amount { get; private set; }
}
