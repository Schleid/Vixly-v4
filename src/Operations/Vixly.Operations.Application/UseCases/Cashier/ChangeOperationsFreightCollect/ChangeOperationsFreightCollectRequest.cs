namespace Vixly.Operations.Application.UseCases.Cashier.ChangeOperationsFreightCollect;

// =========================================================
// FILE: ChangeOperationsFreightCollectRequest.cs
//
// PURPOSE:
// Freight collect or paid here, chosen at the cashier or the counter (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Carry the invoice (cashier) or the shipment (counter), the choice and the reason
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsFreightCollectRequest
{
    public Guid? InvoiceId { get; init; }

    public Guid? ShipmentId { get; init; }

    public bool Collect { get; init; }

    public string? Reason { get; init; }
}
