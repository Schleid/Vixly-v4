using Vixly.Operations.Domain.Api;

namespace Vixly.Operations.Application.UseCases.Api.SaveOperationsWebhookEndpoint;

// =========================================================
// FILE: SaveOperationsWebhookEndpointRequest.cs
//
// PURPOSE:
// Adds or changes a webhook address of the company (Slice 10).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsWebhookEndpointRequest
{
    // None: a new address.
    public Guid? EndpointId { get; init; }

    public string? Url { get; init; }

    public OperationsWebhookEvents Events { get; init; }
}
