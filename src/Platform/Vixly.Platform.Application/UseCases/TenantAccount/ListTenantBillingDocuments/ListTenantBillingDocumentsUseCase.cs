using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.TenantAccount.ListTenantBillingDocuments;

// =========================================================
// FILE: ListTenantBillingDocumentsUseCase.cs
//
// PURPOSE:
// The tenant's invoice, credit note and receipt PDFs, newest first (call 22).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel for the tenant in the service token only (S-13, D-K8)
// - Any tenant role
// =========================================================
public sealed class ListTenantBillingDocumentsUseCase
{
    private readonly IPlatformBillingQueries _queries;

    public ListTenantBillingDocumentsUseCase(
        IPlatformBillingQueries queries)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<ListTenantBillingDocumentsResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        ListTenantBillingDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        return ListTenantBillingDocumentsResponse.Success(await _queries.ListDocumentsAsync(
            context.PlatformTenantId,
            PagedRequest.Create(request.Page, PlatformTenantAccountLimits.PageSize(request.PageSize)),
            cancellationToken));
    }
}
