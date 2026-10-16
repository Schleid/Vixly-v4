using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Billing.ListPlatformInvoices;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Billing;

// =========================================================
// FILE: InvoiceListPresenter.cs
//
// PURPOSE:
// Behaviour of the invoices list.
//
// RESPONSIBILITIES:
// - Load a page of invoices for the chosen filters, for all tenants or one
//
// IMPORTANT NOTES:
// - Filtering and paging happen on the server
// =========================================================
public sealed class InvoiceListPresenter : PlatformPresenter<InvoiceListState>
{
    private readonly ListPlatformInvoicesUseCase _list;

    public InvoiceListPresenter(ListPlatformInvoicesUseCase list, PlatformUseCaseScope scope, ILogger<InvoiceListPresenter> logger)
        : base(scope, InvoiceListState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(PlatformInvoiceStatus? status, bool overdueOnly, Guid? tenantId, string? search, DateTimeOffset? issuedFrom, DateTimeOffset? issuedTo, int page)
    {
        ListPlatformInvoicesResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(
                context,
                new ListPlatformInvoicesRequest
                {
                    Status = status,
                    OverdueOnly = overdueOnly,
                    PlatformTenantId = tenantId,
                    Search = search,
                    IssuedFromUtc = issuedFrom,
                    IssuedToUtc = issuedTo,
                    Page = page
                },
                cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Invoices is { } invoices
            ? new InvoiceListState(invoices.Items, invoices.Page, invoices.TotalPages, invoices.TotalCount, true, null)
            : State with { Loaded = true, Error = error });
    }
}
