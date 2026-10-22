namespace Vixly.Operations.Application.UseCases.Receiving.CloseOperationsDelivery;

// =========================================================
// FILE: CloseOperationsDeliveryRequest.cs
//
// PURPOSE:
// Closes a carrier delivery and issues its warehouse receipt (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CloseOperationsDeliveryRequest
{
    public Guid DeliveryId { get; init; }
}
