using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.CreateOperationsApiKey;

// =========================================================
// FILE: CreateOperationsApiKeyUseCase.cs
//
// PURPOSE:
// Makes an API key for the company’s own programs (Slice 10).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Edit settings and the second check; the plan must include the API
// - The token is shown once and never kept (its hash only); at most 20 active keys
// =========================================================
public sealed class CreateOperationsApiKeyUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsWebhookDesk _webhooks;

    private readonly IOperationsPlanFeatures _features;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CreateOperationsApiKeyUseCase(
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

    public async Task<CreateOperationsApiKeyResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CreateOperationsApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, true, cancellationToken);

        if (refused is not null)
        {
            return CreateOperationsApiKeyResponse.Failure(refused);
        }

        if (!(await _features.GetAsync(context.RequiredTenantId, cancellationToken)).TenantApi)
        {
            return CreateOperationsApiKeyResponse.Failure(OperationsApiErrors.FeatureOff);
        }

        if ((await _webhooks.Store.ListKeysAsync(cancellationToken)).Count(key => key.Active) >= OperationsApiKey.MaxActivePerCompany)
        {
            return CreateOperationsApiKeyResponse.Failure(OperationsApiErrors.TooManyKeys);
        }

        (string keyId, string token, string hash, string hint) = OperationsApiKeys.Generate();
        OperationsApiKey key;

        try
        {
            key = OperationsApiKey.Create(context.RequiredTenantId, request.Name, keyId, hash, hint, access!.User.Id, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return CreateOperationsApiKeyResponse.Failure(exception.Error);
        }

        _webhooks.Store.Add(key);
        _audit.Record(access.User.Id, "ops.api.key_created", nameof(OperationsApiKey), key.Id, after: new { key.Name, key.KeyId });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return CreateOperationsApiKeyResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        // Shown once: only its hash is kept.
        return CreateOperationsApiKeyResponse.Success(token);
    }
}
