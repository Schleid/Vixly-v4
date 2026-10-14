using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Application.UseCases.Tenants.GetPlatformTenant;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantSummaryPresenter.cs
//
// PURPOSE:
// Behaviour of the tenant overview.
//
// RESPONSIBILITIES:
// - Load the tenant and the actor's abilities
// - Reload after a child section changes the tenant
//
// IMPORTANT NOTES:
// - Each section of the page has its own presenter (ADR-0001)
// =========================================================
public sealed class TenantSummaryPresenter : PlatformPresenter<TenantSummaryState>
{
    private readonly GetPlatformTenantUseCase _get;

    public TenantSummaryPresenter(
        GetPlatformTenantUseCase get,
        IPlatformTenantPolicy policy,
        PlatformUseCaseScope scope,
        ILogger<TenantSummaryPresenter> logger)
        : base(scope, TenantSummaryState.Initial with { BaseDomain = policy?.TenantBaseDomain ?? string.Empty }, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        ArgumentNullException.ThrowIfNull(policy);
    }

    public async Task LoadAsync(Guid platformTenantId)
    {
        GetPlatformTenantResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformTenantRequest { PlatformTenantId = platformTenantId }, cancellationToken);

            return response.Error;
        });

        SetState(error is null && response?.Tenant is not null
            ? State with { Tenant = response.Tenant, Abilities = response.Abilities, Version = State.Version + 1, Error = null }
            : State with { Tenant = null, Abilities = PlatformTenantAbilities.None, Error = error });
    }
}
