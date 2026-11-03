using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.RevokeOperationsApiKey;

// =========================================================
// FILE: RevokeOperationsApiKeyUseCase.cs
//
// PURPOSE:
// Revokes an API key at once (Slice 10).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Edit settings; no second check (revoking only takes access away)
// =========================================================
public sealed class RevokeOperationsApiKeyUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsWebhookDesk _webhooks;

    private readonly IOperationsPlanFeatures _features;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RevokeOperationsApiKeyUseCase(
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

    public async Task<RevokeOperationsApiKeyResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RevokeOperationsApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return RevokeOperationsApiKeyResponse.Failure(refused);
        }

        if ((await _webhooks.Store.ListKeysAsync(cancellationToken)).FirstOrDefault(key => key.Id == request.KeyId) is not { } key)
        {
            return RevokeOperationsApiKeyResponse.Failure(OperationsApiErrors.KeyNotFound);
        }

        key.Revoke(access!.User.Id, _timeProvider.GetUtcNow());
        _audit.Record(access.User.Id, "ops.api.key_revoked", nameof(OperationsApiKey), key.Id, before: new { key.Name, key.KeyId });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return RevokeOperationsApiKeyResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        return RevokeOperationsApiKeyResponse.Success();
    }
}
