using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.ResendOperationsWebhookDelivery;

// =========================================================
// FILE: ResendOperationsWebhookDeliveryResponse.cs
//
// PURPOSE:
// The answer to resendOperationsWebhookDelivery.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ResendOperationsWebhookDeliveryResponse
{
    private ResendOperationsWebhookDeliveryResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ResendOperationsWebhookDeliveryResponse Success()
    {
        return new ResendOperationsWebhookDeliveryResponse { Succeeded = true };
    }

    public static ResendOperationsWebhookDeliveryResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ResendOperationsWebhookDeliveryResponse { Error = error };
    }
}
