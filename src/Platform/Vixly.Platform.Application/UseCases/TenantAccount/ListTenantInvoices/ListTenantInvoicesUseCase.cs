using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.TenantAccount.ListTenantInvoices;

// =========================================================
// FILE: ListTenantInvoicesUseCase.cs
//
// PURPOSE:
// The tenant's invoices, newest first (call 22).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel for the tenant in the service token only (S-13, D-K8)
// - Any tenant role
// =========================================================
public sealed class ListTenantInvoicesUseCase
{
    private readonly IPlatformBillingQueries _queries;

    private readonly TimeProvider _timeProvider;

    public ListTenantInvoicesUseCase(
        IPlatformBillingQueries queries,
        TimeProvider timeProvider)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ListTenantInvoicesResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        ListTenantInvoicesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        return ListTenantInvoicesResponse.Success(await _queries.ListInvoicesAsync(
            new PlatformInvoiceListFilter(null, false, context.PlatformTenantId, null, null, null, _timeProvider.GetUtcNow()),
            PagedRequest.Create(request.Page, PlatformTenantAccountLimits.PageSize(request.PageSize)),
            cancellationToken));
    }
}
