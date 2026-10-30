namespace Vixly.Operations.Application.UseCases.Pickup.SendOperationsPickupCode;

// =========================================================
// FILE: SendOperationsPickupCodeRequest.cs
//
// PURPOSE:
// Send the customer a new pickup code (Slice 7 step 2, R-5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SendOperationsPickupCodeRequest
{
    public Guid ShipmentId { get; init; }

    public Guid WarehouseId { get; init; }

    public string? PortalSlug { get; init; }
}
