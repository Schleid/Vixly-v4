using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenantNotes;

// =========================================================
// FILE: ListPlatformTenantNotesUseCase.cs
//
// PURPOSE:
// Reads one page of a tenant's internal notes.
//
// RESPONSIBILITIES:
// - Check platform.tenants.view
// - Return the newest entries first
//
// IMPORTANT NOTES:
// - Paged (P-1)
// =========================================================
public sealed class ListPlatformTenantNotesUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformTenantQueries _queries;

    public ListPlatformTenantNotesUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformTenantQueries queries)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<ListPlatformTenantNotesResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformTenantNotesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.TenantsView, cancellationToken);

        PagedResult<PlatformTenantNoteItem> page = await _queries.ListNotesAsync(
            request.PlatformTenantId,
            PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken);

        return ListPlatformTenantNotesResponse.Success(page);
    }
}
