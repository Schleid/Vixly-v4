using Vixly.Platform.Application.Collections;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Collections;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Collections.CheckPlatformTenantCollections;

// =========================================================
// FILE: CheckPlatformTenantCollectionsUseCase.cs
//
// PURPOSE:
// Looks at one tenant again now, as the clock would (D-C8).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class CheckPlatformTenantCollectionsUseCase
{
    private readonly PlatformCollectionsAdministration _collections;

    public CheckPlatformTenantCollectionsUseCase(
        PlatformCollectionsAdministration collections)
    {
        _collections = collections ?? throw new ArgumentNullException(nameof(collections));
    }

    public async Task<CheckPlatformTenantCollectionsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        CheckPlatformTenantCollectionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _collections.AuthorizeAsync(context, PlatformPermissions.CollectionsManage, requireStepUp: false, cancellationToken) is { } refused)
        {
            return CheckPlatformTenantCollectionsResponse.Failure(refused);
        }

        try
        {
            await _collections.CheckTenantAsync(context, request.PlatformTenantId, cancellationToken);
        }
        catch (DomainException exception)
        {
            _collections.Discard();
            return CheckPlatformTenantCollectionsResponse.Failure(exception.Error);
        }

        if (await _collections.Store.GetOpenCaseAsync(request.PlatformTenantId, cancellationToken) is { } open)
        {
            _collections.Store.AddEvents(new[] { open.RecordCheck(context.PlatformUserId, _collections.UtcNow) });
        }

        _collections.Audit(context, "platform.collections.checked", PlatformPermissions.CollectionsManage, nameof(PlatformTenant), request.PlatformTenantId);

        return await _collections.SaveAsync(cancellationToken)
            ? CheckPlatformTenantCollectionsResponse.Success()
            : CheckPlatformTenantCollectionsResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
