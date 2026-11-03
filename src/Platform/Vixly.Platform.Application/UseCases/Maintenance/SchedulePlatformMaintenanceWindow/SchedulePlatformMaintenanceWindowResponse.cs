using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Maintenance.SchedulePlatformMaintenanceWindow;

// =========================================================
// FILE: SchedulePlatformMaintenanceWindowResponse.cs
//
// PURPOSE:
// Vixly IT publishes a maintenance window: start, end, sites, advance notice and message.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SchedulePlatformMaintenanceWindowResponse
{
    private SchedulePlatformMaintenanceWindowResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid WindowId { get; private init; }

    public string PassToken { get; private init; } = string.Empty;

    public static SchedulePlatformMaintenanceWindowResponse Success(Guid windowId, string passToken)
    {
        return new SchedulePlatformMaintenanceWindowResponse { Succeeded = true, WindowId = windowId, PassToken = passToken };
    }

    public static SchedulePlatformMaintenanceWindowResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SchedulePlatformMaintenanceWindowResponse { Error = error };
    }
}
