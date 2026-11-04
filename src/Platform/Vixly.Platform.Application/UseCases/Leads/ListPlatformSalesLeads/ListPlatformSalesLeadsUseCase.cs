using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Leads.ListPlatformSalesLeads;

// =========================================================
// FILE: ListPlatformSalesLeadsUseCase.cs
//
// PURPOSE:
// Contact requests for sales staff, newest first (Stage 2).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs platform.leads.view
// =========================================================
public sealed class ListPlatformSalesLeadsUseCase
{
    private readonly PlatformLeadAdministration _leads;

    public ListPlatformSalesLeadsUseCase(
        PlatformLeadAdministration leads)
    {
        _leads = leads ?? throw new ArgumentNullException(nameof(leads));
    }

    public async Task<ListPlatformSalesLeadsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformSalesLeadsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _leads.AuthorizeAsync(context, PlatformPermissions.LeadsView, cancellationToken) is { } refused)
        {
            return ListPlatformSalesLeadsResponse.Failure(refused);
        }

        return ListPlatformSalesLeadsResponse.Success(await _leads.Store.ListAsync(
            new PlatformSalesLeadFilter(request.Status, string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim()),
            PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken));
    }
}
