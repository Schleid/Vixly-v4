using Vixly.Platform.Application.Maintenance;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Maintenance;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Maintenance.SchedulePlatformMaintenanceWindow;

// =========================================================
// FILE: SchedulePlatformMaintenanceWindowUseCase.cs
//
// PURPOSE:
// Vixly IT publishes a maintenance window: start, end, sites, advance notice and message.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - platform.maintenance.manage, with step-up
// - The pass link is shown once; only its hash is kept
// - Control is never one of the sites
// =========================================================
public sealed class SchedulePlatformMaintenanceWindowUseCase
{
    private readonly PlatformMaintenanceAdministration _maintenance;

    public SchedulePlatformMaintenanceWindowUseCase(
        PlatformMaintenanceAdministration maintenance)
    {
        _maintenance = maintenance ?? throw new ArgumentNullException(nameof(maintenance));
    }

    public async Task<SchedulePlatformMaintenanceWindowResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SchedulePlatformMaintenanceWindowRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _maintenance.AuthorizeAsync(context, PlatformPermissions.MaintenanceManage, requireStepUp: true, cancellationToken) is { } refused)
        {
            return SchedulePlatformMaintenanceWindowResponse.Failure(refused);
        }

        PlatformMaintenanceSites sites = PlatformMaintenanceSites.None;

        foreach (string code in request.Sites)
        {
            if (PlatformMaintenanceSiteCodes.Parse(code) is not { } site)
            {
                return SchedulePlatformMaintenanceWindowResponse.Failure(PlatformMaintenanceErrors.SitesRequired);
            }

            sites |= site;
        }

        PlatformSecret pass = _maintenance.Secrets.NewToken();
        PlatformMaintenanceWindow window;

        try
        {
            window = PlatformMaintenanceWindow.Schedule(request.StartsUtc, request.EndsUtc, sites, request.NoticeHours,
                request.MessageEn, request.MessageFr, request.MessageEs, pass.Hash, context.PlatformUserId, _maintenance.UtcNow);
        }
        catch (DomainException exception)
        {
            return SchedulePlatformMaintenanceWindowResponse.Failure(exception.Error);
        }

        _maintenance.Store.Add(window);
        _maintenance.Audit.Record(context.PlatformUserId, "platform.maintenance.scheduled", PlatformPermissions.MaintenanceManage, nameof(PlatformMaintenanceWindow), window.Id,
            after: new { starts = window.StartsUtc, ends = window.EndsUtc, sites = PlatformMaintenanceSiteCodes.Of(window.Sites), notice_hours = window.NoticeHours });
        _maintenance.TellEdge(window.Id);

        return await _maintenance.SaveAsync(cancellationToken)
            ? SchedulePlatformMaintenanceWindowResponse.Success(window.Id, pass.Value)
            : SchedulePlatformMaintenanceWindowResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
