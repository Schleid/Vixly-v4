using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.SaveOperationsWebhookEndpoint;

// =========================================================
// FILE: SaveOperationsWebhookEndpointResponse.cs
//
// PURPOSE:
// The answer to saveOperationsWebhookEndpoint.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsWebhookEndpointResponse
{
    private SaveOperationsWebhookEndpointResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string? Secret { get; private init; }

    public static SaveOperationsWebhookEndpointResponse Success(string? secret)
    {
        return new SaveOperationsWebhookEndpointResponse { Succeeded = true, Secret = secret };
    }

    public static SaveOperationsWebhookEndpointResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsWebhookEndpointResponse { Error = error };
    }
}
