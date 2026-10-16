using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.ListPlatformInvoices;

// =========================================================
// FILE: ListPlatformInvoicesUseCase.cs
//
// PURPOSE:
// Lists invoices, paged, with status, overdue, tenant, number or company and date filters (D-B3).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListPlatformInvoicesUseCase
{
    private readonly PlatformBillingAdministration _billing;

    private readonly IPlatformBillingQueries _queries;

    public ListPlatformInvoicesUseCase(
        PlatformBillingAdministration billing,
        IPlatformBillingQueries queries)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<ListPlatformInvoicesResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformInvoicesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.BillingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return ListPlatformInvoicesResponse.Failure(refused);
        }

        if (request.Status is { } status && !Enum.IsDefined(status))
        {
            return ListPlatformInvoicesResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        PagedResult<PlatformInvoiceListItem> invoices = await _queries.ListInvoicesAsync(
            new PlatformInvoiceListFilter(request.Status, request.OverdueOnly, request.PlatformTenantId, search is { Length: > 100 } ? search[..100] : search, request.IssuedFromUtc, request.IssuedToUtc, _billing.UtcNow),
            PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken);

        return ListPlatformInvoicesResponse.Success(invoices, await PlatformBillingAbilities.ReadAsync(_billing, context, cancellationToken));
    }
}
