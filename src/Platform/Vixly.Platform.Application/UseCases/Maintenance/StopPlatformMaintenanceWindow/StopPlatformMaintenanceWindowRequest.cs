namespace Vixly.Platform.Application.UseCases.Maintenance.StopPlatformMaintenanceWindow;

// =========================================================
// FILE: StopPlatformMaintenanceWindowRequest.cs
//
// PURPOSE:
// Cancels a window before it starts, or ends a running one now.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class StopPlatformMaintenanceWindowRequest
{
    public Guid WindowId { get; init; }
}
