using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.ResendOperationsWebhookDelivery;

// =========================================================
// FILE: ResendOperationsWebhookDeliveryUseCase.cs
//
// PURPOSE:
// Sends a delivery again, with a fresh series of tries (Slice 10).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Edit settings; the plan must include the API
// - The worker sends it on its next round, with the same event id
// =========================================================
public sealed class ResendOperationsWebhookDeliveryUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsWebhookDesk _webhooks;

    private readonly IOperationsPlanFeatures _features;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ResendOperationsWebhookDeliveryUseCase(
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

    public async Task<ResendOperationsWebhookDeliveryResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ResendOperationsWebhookDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return ResendOperationsWebhookDeliveryResponse.Failure(refused);
        }

        if (!(await _features.GetAsync(context.RequiredTenantId, cancellationToken)).TenantApi)
        {
            return ResendOperationsWebhookDeliveryResponse.Failure(OperationsApiErrors.FeatureOff);
        }

        if (await _webhooks.Store.GetDeliveryAsync(request.DeliveryId, cancellationToken) is not { } delivery)
        {
            return ResendOperationsWebhookDeliveryResponse.Failure(OperationsApiErrors.DeliveryNotFound);
        }

        delivery.Resend(_timeProvider.GetUtcNow());

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return ResendOperationsWebhookDeliveryResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        return ResendOperationsWebhookDeliveryResponse.Success();
    }
}
