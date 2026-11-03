namespace Vixly.Operations.Application.UseCases.Api.ResendOperationsWebhookDelivery;

// =========================================================
// FILE: ResendOperationsWebhookDeliveryRequest.cs
//
// PURPOSE:
// Sends a delivery again, with a fresh series of tries (Slice 10).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ResendOperationsWebhookDeliveryRequest
{
    public Guid DeliveryId { get; init; }
}
