using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.SendOperationsWebhookTest;

// =========================================================
// FILE: SendOperationsWebhookTestUseCase.cs
//
// PURPOSE:
// Sends a test event to a webhook address now (Slice 10).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Edit settings; the plan must include the API
// - Sent at once and kept in the log; tried once only, and never counted against the address
// =========================================================
public sealed class SendOperationsWebhookTestUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsWebhookDesk _webhooks;

    private readonly IOperationsPlanFeatures _features;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SendOperationsWebhookTestUseCase(
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

    public async Task<SendOperationsWebhookTestResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SendOperationsWebhookTestRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return SendOperationsWebhookTestResponse.Failure(refused);
        }

        if (!(await _features.GetAsync(context.RequiredTenantId, cancellationToken)).TenantApi)
        {
            return SendOperationsWebhookTestResponse.Failure(OperationsApiErrors.FeatureOff);
        }

        if (await _webhooks.Store.GetEndpointAsync(request.EndpointId, cancellationToken) is not { Active: true } endpoint)
        {
            return SendOperationsWebhookTestResponse.Failure(OperationsApiErrors.EndpointNotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        string eventId = "evt_test_" + Guid.NewGuid().ToString("N");
        OperationsWebhookDelivery delivery = OperationsWebhookDelivery.Create(context.RequiredTenantId, endpoint.Id, eventId, OperationsWebhookEventNames.Test,
            OperationsWebhookDesk.Body(eventId, OperationsWebhookEventNames.Test, utcNow, new { endpointId = endpoint.Id }), utcNow);
        _webhooks.Store.Add(delivery);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return SendOperationsWebhookTestResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        // Sent and recorded by the desk's own conditional updates: read back fresh, not this tracked copy.
        _unitOfWork.DiscardChanges();
        await _webhooks.SendAsync(delivery.Id, cancellationToken);
        OperationsWebhookDelivery? sent = await _webhooks.Store.GetDeliveryAsync(delivery.Id, cancellationToken);
        return SendOperationsWebhookTestResponse.Success(sent?.Status == OperationsWebhookStatus.Delivered, sent?.LastStatusCode, sent?.LastError);
    }
}
