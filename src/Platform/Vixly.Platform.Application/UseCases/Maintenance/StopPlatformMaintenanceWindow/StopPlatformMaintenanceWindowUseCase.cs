using Vixly.Platform.Application.Maintenance;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Maintenance;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Maintenance.StopPlatformMaintenanceWindow;

// =========================================================
// FILE: StopPlatformMaintenanceWindowUseCase.cs
//
// PURPOSE:
// Cancels a window before it starts, or ends a running one now.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - platform.maintenance.manage, with step-up
// - A finished or cancelled window cannot be stopped again
// =========================================================
public sealed class StopPlatformMaintenanceWindowUseCase
{
    private readonly PlatformMaintenanceAdministration _maintenance;

    public StopPlatformMaintenanceWindowUseCase(
        PlatformMaintenanceAdministration maintenance)
    {
        _maintenance = maintenance ?? throw new ArgumentNullException(nameof(maintenance));
    }

    public async Task<StopPlatformMaintenanceWindowResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        StopPlatformMaintenanceWindowRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _maintenance.AuthorizeAsync(context, PlatformPermissions.MaintenanceManage, requireStepUp: true, cancellationToken) is { } refused)
        {
            return StopPlatformMaintenanceWindowResponse.Failure(refused);
        }

        if (await _maintenance.Store.GetAsync(request.WindowId, cancellationToken) is not { } window)
        {
            return StopPlatformMaintenanceWindowResponse.Failure(PlatformMaintenanceErrors.NotFound);
        }

        try
        {
            window.Stop(context.PlatformUserId, _maintenance.UtcNow);
        }
        catch (DomainException exception)
        {
            return StopPlatformMaintenanceWindowResponse.Failure(exception.Error);
        }

        _maintenance.Audit.Record(context.PlatformUserId, "platform.maintenance.stopped", PlatformPermissions.MaintenanceManage, nameof(PlatformMaintenanceWindow), window.Id,
            after: new { cancelled = window.CancelledUtc is not null, ends = window.EndsUtc });
        _maintenance.TellEdge(window.Id);

        return await _maintenance.SaveAsync(cancellationToken)
            ? StopPlatformMaintenanceWindowResponse.Success()
            : StopPlatformMaintenanceWindowResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
