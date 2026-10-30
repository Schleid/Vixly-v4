namespace Vixly.Operations.Application.UseCases.Counter.AddOperationsCounterSupply;

// =========================================================
// FILE: AddOperationsCounterSupplyRequest.cs
//
// PURPOSE:
// A supply added at the counter (Slice 5E, S-11): by scan or by name, to the shipment being processed or to an order of supplies.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AddOperationsCounterSupplyRequest
{
    // The shipment being processed, or the order (SaleId).
    public Guid? ShipmentId { get; init; }

    public Guid? SaleId { get; init; }

    // What was scanned or typed: a barcode or an item code finds one supply at once; a name may find several to choose from.
    public string? Scan { get; init; }

    // The supply chosen from the list.
    public Guid? SupplyId { get; init; }

    public int Quantity { get; init; } = 1;

    // The form's key: sent twice (a double click), applied once (ADR-0019).
    public Guid Key { get; init; }
}
