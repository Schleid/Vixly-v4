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
// FILE: GetOperationsApiSettingsUseCase.cs
//
// PURPOSE:
// Settings → API and webhooks (Slice 10): the keys, the addresses and the latest deliveries.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Whoever may see settings; the secrets are never here (only their last characters)
// =========================================================
public sealed class GetOperationsApiSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsWebhookDesk _webhooks;

    private readonly IOperationsPlanFeatures _features;

    public GetOperationsApiSettingsUseCase(
        OperationsAccess access,
        OperationsWebhookDesk webhooks,
        IOperationsPlanFeatures features)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _webhooks = webhooks ?? throw new ArgumentNullException(nameof(webhooks));
        _features = features ?? throw new ArgumentNullException(nameof(features));
    }

    public async Task<GetOperationsApiSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsApiSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsApiSettingsResponse.Failure(refused);
        }

        return GetOperationsApiSettingsResponse.Success((await _features.GetAsync(context.RequiredTenantId, cancellationToken)).TenantApi,
            await _webhooks.Store.ListKeysAsync(cancellationToken), await _webhooks.Store.ListEndpointsAsync(cancellationToken),
            await _webhooks.Store.ListDeliveriesAsync(100, cancellationToken), access!.Has(OperationsPermissions.SettingsEdit));
    }
}
