using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Leads.ListPlatformSalesLeads;
using Vixly.Platform.Domain.Sales;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: LeadsPresenter.cs
//
// PURPOSE:
// Behaviour of the leads list: contact requests from the public site.
//
// RESPONSIBILITIES:
// - Load one page of leads with the chosen filters
//
// IMPORTANT NOTES:
// - Read only; updates happen on the lead page
// =========================================================
public sealed class LeadsPresenter : PlatformPresenter<LeadsState>
{
    private readonly ListPlatformSalesLeadsUseCase _list;

    public LeadsPresenter(ListPlatformSalesLeadsUseCase list, PlatformUseCaseScope scope, ILogger<LeadsPresenter> logger)
        : base(scope, LeadsState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(PlatformSalesLeadStatus? status, string? search, int page)
    {
        ListPlatformSalesLeadsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(context, new ListPlatformSalesLeadsRequest { Status = status, Search = search, Page = page }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Leads is { } result
            ? new LeadsState(result.Items, result.Page, result.TotalPages, result.TotalCount, true, null)
            : State with { Loaded = true, Error = error });
    }
}
