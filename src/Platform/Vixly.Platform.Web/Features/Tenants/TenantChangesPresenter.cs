using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenantChanges;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantChangesPresenter.cs
//
// PURPOSE:
// Loads the company change queue for staff (B12).
//
// RESPONSIBILITIES:
// - Run the list use case and keep the page
//
// IMPORTANT NOTES:
// - Pending requests by default
// =========================================================
public sealed class TenantChangesPresenter : PlatformPresenter<TenantChangesState>
{
    private readonly ListPlatformTenantChangesUseCase _list;

    public TenantChangesPresenter(ListPlatformTenantChangesUseCase list, PlatformUseCaseScope scope, ILogger<TenantChangesPresenter> logger)
        : base(scope, TenantChangesState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(PlatformTenantChangeStatus? status, Guid? tenantId, int page)
    {
        ListPlatformTenantChangesResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(context, new ListPlatformTenantChangesRequest { Status = status, PlatformTenantId = tenantId, Page = page }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Changes is { } result
            ? new TenantChangesState(result.Items, result.Page, result.TotalPages, result.TotalCount, true, null)
            : State with { Loaded = true, Error = error });
    }
}
