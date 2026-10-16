using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Collections.GetPlatformTenantCollections;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Collections;

// =========================================================
// FILE: TenantCollectionsPresenter.cs
//
// PURPOSE:
// Behaviour of the Collections part of the tenant page.
//
// RESPONSIBILITIES:
// - Load the tenant's open case and overdue invoices
//
// IMPORTANT NOTES:
// - Read only; actions live on the case page
// =========================================================
public sealed class TenantCollectionsPresenter : PlatformPresenter<TenantCollectionsState>
{
    private readonly GetPlatformTenantCollectionsUseCase _get;

    public TenantCollectionsPresenter(GetPlatformTenantCollectionsUseCase get, PlatformUseCaseScope scope, ILogger<TenantCollectionsPresenter> logger)
        : base(scope, TenantCollectionsState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
    }

    public async Task LoadAsync(Guid tenantId)
    {
        GetPlatformTenantCollectionsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformTenantCollectionsRequest { PlatformTenantId = tenantId }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? new TenantCollectionsState(response.Summary, true, null)
            : State with { Loaded = true, Error = error });
    }
}
