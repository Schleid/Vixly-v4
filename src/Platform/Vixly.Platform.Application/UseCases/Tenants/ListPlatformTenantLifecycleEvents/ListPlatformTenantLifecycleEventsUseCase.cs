using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenantLifecycleEvents;

// =========================================================
// FILE: ListPlatformTenantLifecycleEventsUseCase.cs
//
// PURPOSE:
// Reads one page of a tenant's history.
//
// RESPONSIBILITIES:
// - Check platform.tenants.view
// - Return the newest entries first
//
// IMPORTANT NOTES:
// - Paged (P-1)
// =========================================================
public sealed class ListPlatformTenantLifecycleEventsUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformTenantQueries _queries;

    public ListPlatformTenantLifecycleEventsUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformTenantQueries queries)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<ListPlatformTenantLifecycleEventsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformTenantLifecycleEventsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.TenantsView, cancellationToken);

        PagedResult<PlatformTenantEventItem> page = await _queries.ListEventsAsync(
            request.PlatformTenantId,
            PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken);

        return ListPlatformTenantLifecycleEventsResponse.Success(page);
    }
}
