using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenantChanges;

// =========================================================
// FILE: ListPlatformTenantChangesUseCase.cs
//
// PURPOSE:
// Lists company change requests for staff, pending first by default (B12).
//
// RESPONSIBILITIES:
// - Check who may act first, then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListPlatformTenantChangesUseCase
{
    private readonly PlatformTenantChanges _changes;

    public ListPlatformTenantChangesUseCase(
        PlatformTenantChanges changes)
    {
        _changes = changes ?? throw new ArgumentNullException(nameof(changes));
    }

    public async Task<ListPlatformTenantChangesResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformTenantChangesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _changes.AuthorizeAsync(context, PlatformPermissions.TenantsView, false, cancellationToken);
        PagedRequest page = PagedRequest.Create(Math.Max(1, request.Page), Math.Clamp(request.PageSize, 1, 100));

        return ListPlatformTenantChangesResponse.Success(
            await _changes.Requests.ListAsync(new PlatformTenantChangeFilter(request.Status, request.PlatformTenantId), page, cancellationToken));
    }
}
