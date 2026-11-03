namespace Vixly.Operations.Application.UseCases.Api.SendOperationsWebhookTest;

// =========================================================
// FILE: SendOperationsWebhookTestRequest.cs
//
// PURPOSE:
// Sends a test event to a webhook address now (Slice 10).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SendOperationsWebhookTestRequest
{
    public Guid EndpointId { get; init; }
}
