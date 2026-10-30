using Vixly.Operations.Application.Shipments;

namespace Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipmentChoices;

// =========================================================
// FILE: GetMyOperationsShipmentChoicesRequest.cs
//
// PURPOSE:
// What a customer may choose when creating a shipment (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsShipmentChoicesRequest
{
    public string SessionToken { get; init; } = string.Empty;
}
