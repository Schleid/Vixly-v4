using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.SwitchOperationsWebhookEndpoint;

// =========================================================
// FILE: SwitchOperationsWebhookEndpointResponse.cs
//
// PURPOSE:
// The answer to switchOperationsWebhookEndpoint.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SwitchOperationsWebhookEndpointResponse
{
    private SwitchOperationsWebhookEndpointResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SwitchOperationsWebhookEndpointResponse Success()
    {
        return new SwitchOperationsWebhookEndpointResponse { Succeeded = true };
    }

    public static SwitchOperationsWebhookEndpointResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SwitchOperationsWebhookEndpointResponse { Error = error };
    }
}
