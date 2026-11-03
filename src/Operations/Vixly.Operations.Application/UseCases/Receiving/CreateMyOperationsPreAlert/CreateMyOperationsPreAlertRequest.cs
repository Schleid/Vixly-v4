namespace Vixly.Operations.Application.UseCases.Receiving.CreateMyOperationsPreAlert;

// =========================================================
// FILE: CreateMyOperationsPreAlertRequest.cs
//
// PURPOSE:
// A customer announces one more package in one of their shipments (Slice 3B; owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CreateMyOperationsPreAlertRequest
{
    public string SessionToken { get; init; } = string.Empty;

    // Slice 4D: optional; without it the pre-alert is in no shipment yet.
    public Guid? ShipmentId { get; init; }

    // Slice 4D: the warehouse the package is shipped to; may be left out when the customer has only one.
    public Guid? WarehouseId { get; init; }

    public string? Carrier { get; init; }

    public string? TrackingNumber { get; init; }

    public string? Store { get; init; }

    public string? Contents { get; init; }

    public decimal DeclaredValue { get; init; }

    public DateOnly? ExpectedDate { get; init; }

    // Owner, 2026-11-01: the items, one per line; none: Contents and DeclaredValue as before.
    public IReadOnlyList<Vixly.Operations.Application.Items.OperationsItemDraft> Items { get; init; } = Array.Empty<Vixly.Operations.Application.Items.OperationsItemDraft>();
}
