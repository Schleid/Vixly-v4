using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.GetOperationsApiSettings;

// =========================================================
// FILE: GetOperationsApiSettingsResponse.cs
//
// PURPOSE:
// The answer to getOperationsApiSettings.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsApiSettingsResponse
{
    private GetOperationsApiSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool InPlan { get; private init; }

    public IReadOnlyList<OperationsApiKey> Keys { get; private init; } = Array.Empty<OperationsApiKey>();

    public IReadOnlyList<OperationsWebhookEndpoint> Endpoints { get; private init; } = Array.Empty<OperationsWebhookEndpoint>();

    public IReadOnlyList<OperationsWebhookDelivery> Deliveries { get; private init; } = Array.Empty<OperationsWebhookDelivery>();

    public bool CanEdit { get; private init; }

    public static GetOperationsApiSettingsResponse Success(bool inPlan, IReadOnlyList<OperationsApiKey> keys, IReadOnlyList<OperationsWebhookEndpoint> endpoints, IReadOnlyList<OperationsWebhookDelivery> deliveries, bool canEdit)
    {
        return new GetOperationsApiSettingsResponse { Succeeded = true, InPlan = inPlan, Keys = keys, Endpoints = endpoints, Deliveries = deliveries, CanEdit = canEdit };
    }

    public static GetOperationsApiSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsApiSettingsResponse { Error = error };
    }
}
