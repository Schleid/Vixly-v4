using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.SwitchOperationsWebhookEndpoint;

// =========================================================
// FILE: SwitchOperationsWebhookEndpointUseCase.cs
//
// PURPOSE:
// Turns a webhook address off or back on, or removes it (Slice 10).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Edit settings
// - Back on, it starts from now; removed, its deliveries go with it
// =========================================================
public sealed class SwitchOperationsWebhookEndpointUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsWebhookDesk _webhooks;

    private readonly IOperationsPlanFeatures _features;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SwitchOperationsWebhookEndpointUseCase(
        OperationsAccess access,
        OperationsWebhookDesk webhooks,
        IOperationsPlanFeatures features,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _webhooks = webhooks ?? throw new ArgumentNullException(nameof(webhooks));
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SwitchOperationsWebhookEndpointResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SwitchOperationsWebhookEndpointRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return SwitchOperationsWebhookEndpointResponse.Failure(refused);
        }

        if (await _webhooks.Store.GetEndpointAsync(request.EndpointId, cancellationToken) is not { } endpoint)
        {
            return SwitchOperationsWebhookEndpointResponse.Failure(OperationsApiErrors.EndpointNotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        switch (request.Action)
        {
            case "on":
                endpoint.Enable(await _webhooks.Store.CurrentVersionAsync(cancellationToken), utcNow);
                break;
            case "off":
                endpoint.Disable(utcNow);
                break;
            case "remove":
                _webhooks.Store.Remove(endpoint);
                break;
            default:
                return SwitchOperationsWebhookEndpointResponse.Failure(OperationsCommonErrors.NotFound);
        }

        _audit.Record(access!.User.Id, "ops.api.webhook_" + (request.Action == "remove" ? "removed" : request.Action == "on" ? "enabled" : "disabled"), nameof(OperationsWebhookEndpoint),
            endpoint.Id, before: new { endpoint.Url });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return SwitchOperationsWebhookEndpointResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        return SwitchOperationsWebhookEndpointResponse.Success();
    }
}
