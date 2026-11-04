using Vixly.Platform.Application.Maintenance;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Maintenance;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Maintenance.ListPlatformMaintenanceWindows;

// =========================================================
// FILE: ListPlatformMaintenanceWindowsUseCase.cs
//
// PURPOSE:
// Control's maintenance page: the windows to come and running, then the past ones.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - platform.maintenance.view
// - Open: not cancelled and not finished, soonest first
// =========================================================
public sealed class ListPlatformMaintenanceWindowsUseCase
{
    private readonly PlatformMaintenanceAdministration _maintenance;

    private readonly IPlatformAuthorizationService _authorization;

    public ListPlatformMaintenanceWindowsUseCase(
        PlatformMaintenanceAdministration maintenance,
        IPlatformAuthorizationService authorization)
    {
        _maintenance = maintenance ?? throw new ArgumentNullException(nameof(maintenance));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    public async Task<ListPlatformMaintenanceWindowsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformMaintenanceWindowsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _maintenance.AuthorizeAsync(context, PlatformPermissions.MaintenanceView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return ListPlatformMaintenanceWindowsResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _maintenance.UtcNow;
        IReadOnlyList<PlatformMaintenanceWindow> open = await _maintenance.Store.ListOpenAsync(utcNow, cancellationToken);
        PagedResult<PlatformMaintenanceWindow> past = await _maintenance.Store.ListPastAsync(utcNow, PagedRequest.Create(request.Page, request.PageSize), cancellationToken);
        bool canManage = await _authorization.IsAuthorizedAsync(context, PlatformPermissions.MaintenanceManage, cancellationToken);

        return ListPlatformMaintenanceWindowsResponse.Success(
            open.Select(window => PlatformMaintenanceWindowItem.Of(window, utcNow)).ToList(),
            new PagedResult<PlatformMaintenanceWindowItem>(past.Items.Select(window => PlatformMaintenanceWindowItem.Of(window, utcNow)).ToList(), PagedRequest.Create(past.Page, past.PageSize), past.TotalCount),
            canManage);
    }
}
