using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Maintenance.StopPlatformMaintenanceWindow;

// =========================================================
// FILE: StopPlatformMaintenanceWindowResponse.cs
//
// PURPOSE:
// Cancels a window before it starts, or ends a running one now.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class StopPlatformMaintenanceWindowResponse
{
    private StopPlatformMaintenanceWindowResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static StopPlatformMaintenanceWindowResponse Success()
    {
        return new StopPlatformMaintenanceWindowResponse { Succeeded = true };
    }

    public static StopPlatformMaintenanceWindowResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new StopPlatformMaintenanceWindowResponse { Error = error };
    }
}
