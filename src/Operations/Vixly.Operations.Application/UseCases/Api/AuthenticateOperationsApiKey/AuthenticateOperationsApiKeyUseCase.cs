using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.AuthenticateOperationsApiKey;

// =========================================================
// FILE: AuthenticateOperationsApiKeyUseCase.cs
//
// PURPOSE:
// Checks a company API key on a request (Slice 10).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - No signed-in person: the tenant comes from the host, the trust from the key
// - A revoked key, another company’s key or a wrong secret are refused alike
// =========================================================
public sealed class AuthenticateOperationsApiKeyUseCase
{
    private readonly OperationsWebhookDesk _webhooks;

    private readonly IOperationsPlanFeatures _features;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AuthenticateOperationsApiKeyUseCase(
        OperationsWebhookDesk webhooks,
        IOperationsPlanFeatures features,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _webhooks = webhooks ?? throw new ArgumentNullException(nameof(webhooks));
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AuthenticateOperationsApiKeyResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        AuthenticateOperationsApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // The key must belong to the company of the address called: it is looked up in its tenant only.
        if (context.TenantId is null || !OperationsApiKeys.TryRead(request.Token, out string keyId, out string secret)
            || await _webhooks.Store.FindKeyAsync(keyId, cancellationToken) is not { Active: true } key || !OperationsApiKeys.Matches(secret, key.SecretHash))
        {
            return AuthenticateOperationsApiKeyResponse.Failure(OperationsApiErrors.KeyNotFound);
        }

        if (!(await _features.GetAsync(context.TenantId.Value, cancellationToken)).TenantApi)
        {
            return AuthenticateOperationsApiKeyResponse.Failure(OperationsApiErrors.FeatureOff);
        }

        if (key.Used(_timeProvider.GetUtcNow()) && !await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
        }

        return AuthenticateOperationsApiKeyResponse.Success(key.KeyId, key.Name);
    }
}
