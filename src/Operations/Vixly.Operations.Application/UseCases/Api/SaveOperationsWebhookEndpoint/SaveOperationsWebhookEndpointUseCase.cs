using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.SaveOperationsWebhookEndpoint;

// =========================================================
// FILE: SaveOperationsWebhookEndpointUseCase.cs
//
// PURPOSE:
// Adds or changes a webhook address of the company (Slice 10).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Edit settings and the second check (an address receives the company’s data); the plan must include the API
// - HTTPS to a public host only; the signing secret is shown once, when the address is added
// =========================================================
public sealed class SaveOperationsWebhookEndpointUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsWebhookDesk _webhooks;

    private readonly IOperationsPlanFeatures _features;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly IOperationsSecretProtector _protector;

    public SaveOperationsWebhookEndpointUseCase(
        OperationsAccess access,
        OperationsWebhookDesk webhooks,
        IOperationsPlanFeatures features,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IOperationsSecretProtector protector)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _webhooks = webhooks ?? throw new ArgumentNullException(nameof(webhooks));
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
    }

    public async Task<SaveOperationsWebhookEndpointResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsWebhookEndpointRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, true, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsWebhookEndpointResponse.Failure(refused);
        }

        if (!(await _features.GetAsync(context.RequiredTenantId, cancellationToken)).TenantApi)
        {
            return SaveOperationsWebhookEndpointResponse.Failure(OperationsApiErrors.FeatureOff);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        long version = await _webhooks.Store.CurrentVersionAsync(cancellationToken);
        IReadOnlyList<OperationsWebhookEndpoint> endpoints = await _webhooks.Store.ListEndpointsAsync(cancellationToken);
        string? secret = null;
        OperationsWebhookEndpoint endpoint;

        try
        {
            if (request.EndpointId is { } id)
            {
                endpoint = endpoints.FirstOrDefault(item => item.Id == id) ?? throw new DomainException(OperationsApiErrors.EndpointNotFound);
                endpoint.Change(request.Url, request.Events, version, utcNow);
            }
            else
            {
                if (endpoints.Count >= OperationsWebhookEndpoint.MaxPerCompany)
                {
                    return SaveOperationsWebhookEndpointResponse.Failure(OperationsApiErrors.TooManyEndpoints);
                }

                secret = OperationsWebhookSignature.NewSecret();
                endpoint = OperationsWebhookEndpoint.Create(context.RequiredTenantId, request.Url, request.Events, _protector.Protect(secret, OperationsSecretPurposes.WebhookSecret),
                    secret[^4..], access!.User.Id, version, utcNow);
                _webhooks.Store.Add(endpoint);
            }
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return SaveOperationsWebhookEndpointResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, secret is null ? "ops.api.webhook_changed" : "ops.api.webhook_added", nameof(OperationsWebhookEndpoint), endpoint.Id,
            after: new { endpoint.Url, events = endpoint.Events.ToString() });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return SaveOperationsWebhookEndpointResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        // A new address: its signing secret, shown once.
        return SaveOperationsWebhookEndpointResponse.Success(secret);
    }
}
