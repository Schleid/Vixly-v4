using Vixly.Operations.Domain.Pickup;

namespace Vixly.Operations.Application.UseCases.Pickup.ReleaseOperationsPickup;

// =========================================================
// FILE: ReleaseOperationsPickupRequest.cs
//
// PURPOSE:
// Release a shipment's parcels ready at the pickup counter, once paid and with the proof the company asks (Slice 7 step 2, R-5, R-6, R-9).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReleaseOperationsPickupRequest
{
    public Guid ShipmentId { get; init; }

    public Guid WarehouseId { get; init; }

    public OperationsPickupCollector Collector { get; init; }

    public OperationsPickupIdKind? IdKind { get; init; }

    public string? IdLast { get; init; }

    public byte[]? Signature { get; init; }

    public byte[]? Photo { get; init; }

    public string? Code { get; init; }

    public Guid? RequestKey { get; init; }
}
