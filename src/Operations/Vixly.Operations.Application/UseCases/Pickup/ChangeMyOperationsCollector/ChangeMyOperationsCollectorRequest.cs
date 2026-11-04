namespace Vixly.Operations.Application.UseCases.Pickup.ChangeMyOperationsCollector;

// =========================================================
// FILE: ChangeMyOperationsCollectorRequest.cs
//
// PURPOSE:
// The customer names who may collect a shipment, or no one (Slice 7 step 2, R-6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeMyOperationsCollectorRequest
{
    public string? SessionToken { get; init; }

    public Guid ShipmentId { get; init; }

    public string? Name { get; init; }

    public string? Phone { get; init; }
}
