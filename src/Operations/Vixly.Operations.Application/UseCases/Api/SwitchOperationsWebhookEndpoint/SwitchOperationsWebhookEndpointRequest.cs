namespace Vixly.Operations.Application.UseCases.Api.SwitchOperationsWebhookEndpoint;

// =========================================================
// FILE: SwitchOperationsWebhookEndpointRequest.cs
//
// PURPOSE:
// Turns a webhook address off or back on, or removes it (Slice 10).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SwitchOperationsWebhookEndpointRequest
{
    public Guid EndpointId { get; init; }

    // On, off or remove.
    public string Action { get; init; } = string.Empty;
}
