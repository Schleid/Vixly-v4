using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenants;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantListPresenter.cs
//
// PURPOSE:
// Behaviour of the tenant directory.
//
// RESPONSIBILITIES:
// - Load one filtered page of tenants
//
// IMPORTANT NOTES:
// - Filters and page come from the URL, so lists can be shared
// =========================================================
public sealed class TenantListPresenter : PlatformPresenter<TenantListState>
{
    private readonly ListPlatformTenantsUseCase _list;

    private readonly IPlatformTenantPolicy _policy;

    public TenantListPresenter(
        ListPlatformTenantsUseCase list,
        IPlatformTenantPolicy policy,
        PlatformUseCaseScope scope,
        ILogger<TenantListPresenter> logger)
        : base(scope, TenantListState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public async Task LoadAsync(string? search, PlatformTenantStatus? status, string? tag, int page)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            ListPlatformTenantsResponse response = await _list.ExecuteAsync(
                context,
                new ListPlatformTenantsRequest { Search = search, Status = status, Tag = tag, Page = page },
                cancellationToken);

            if (response.Tenants is null)
            {
                return response.Error;
            }

            SetState(new TenantListState(
                response.Tenants.Items,
                response.Tenants.Page,
                response.Tenants.TotalPages,
                response.Tenants.TotalCount,
                response.CanCreate,
                _policy.TenantBaseDomain,
                true,
                null));

            return null;
        });

        if (error is not null)
        {
            SetState(TenantListState.Initial with { Loaded = true, Error = error });
        }
    }
}
