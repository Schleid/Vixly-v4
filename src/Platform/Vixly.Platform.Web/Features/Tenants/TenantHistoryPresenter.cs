using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenantLifecycleEvents;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantHistoryPresenter.cs
//
// PURPOSE:
// Behaviour of the history section.
//
// RESPONSIBILITIES:
// - Page through history events, newest first
//
// IMPORTANT NOTES:
// - Read-only
// =========================================================
public sealed class TenantHistoryPresenter : PlatformPresenter<TenantHistoryState>
{
    private readonly ListPlatformTenantLifecycleEventsUseCase _list;

    public TenantHistoryPresenter(ListPlatformTenantLifecycleEventsUseCase list, PlatformUseCaseScope scope, ILogger<TenantHistoryPresenter> logger)
        : base(scope, TenantHistoryState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(Guid platformTenantId, int page)
    {
        ListPlatformTenantLifecycleEventsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(
                context,
                new ListPlatformTenantLifecycleEventsRequest { PlatformTenantId = platformTenantId, Page = page, PageSize = 10 },
                cancellationToken);

            return response.Error;
        });

        SetState(error is null && response?.Events is { } events
            ? new TenantHistoryState(events.Items, events.Page, events.TotalPages, true, null)
            : State with { Loaded = true, Error = error });
    }
}
