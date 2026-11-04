using Vixly.Operations.Application.Shipments;

namespace Vixly.Operations.Application.UseCases.Shipments.CreateMyOperationsShipment;

// =========================================================
// FILE: CreateMyOperationsShipmentRequest.cs
//
// PURPOSE:
// A customer creates a shipment: where it goes, who receives it, the options, and its first package (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CreateMyOperationsShipmentRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid OriginWarehouseId { get; init; }

    public OperationsShipmentDraft Instructions { get; init; } = null!;

    // Slice 4D: optional; a shipment needs no pre-alert (goods the customer drops off; owner, 2026-10-19).
    public OperationsPackageDraft? Package { get; init; }

    // Slice 4D: the customer's packages put into it at once, by pre-alert or by parcel.
    public IReadOnlyList<Guid> PreAlertIds { get; init; } = Array.Empty<Guid>();

    public IReadOnlyList<Guid> ParcelIds { get; init; } = Array.Empty<Guid>();

    // Slice 4D: the form's reference; the same form sent twice creates one shipment (ADR-0019).
    public string? RequestKey { get; init; }
}
