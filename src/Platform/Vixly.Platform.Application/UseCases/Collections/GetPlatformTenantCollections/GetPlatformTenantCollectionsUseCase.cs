using Vixly.Platform.Application.Collections;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Collections;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Collections.GetPlatformTenantCollections;

// =========================================================
// FILE: GetPlatformTenantCollectionsUseCase.cs
//
// PURPOSE:
// Reads the Collections section of a tenant page: the open case and overdue invoices.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformTenantCollectionsUseCase
{
    private readonly PlatformCollectionsAdministration _collections;

    public GetPlatformTenantCollectionsUseCase(
        PlatformCollectionsAdministration collections)
    {
        _collections = collections ?? throw new ArgumentNullException(nameof(collections));
    }

    public async Task<GetPlatformTenantCollectionsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformTenantCollectionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _collections.AuthorizeAsync(context, PlatformPermissions.CollectionsView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformTenantCollectionsResponse.Failure(refused);
        }

        PlatformCollectionCase? open = await _collections.Store.GetOpenCaseAsync(request.PlatformTenantId, cancellationToken);

        return GetPlatformTenantCollectionsResponse.Success(new PlatformTenantCollectionsSummary(
            open is null ? null : await _collections.Queries.GetCaseAsync(open.Id, _collections.UtcNow, cancellationToken),
            await _collections.Queries.ListOverdueInvoicesAsync(request.PlatformTenantId, _collections.UtcNow, cancellationToken),
            await PlatformCollectionsAbilities.ForAsync(_collections, context, open, cancellationToken)));
    }
}
