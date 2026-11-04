namespace Vixly.Platform.Application.UseCases.Maintenance.CheckPlatformMaintenancePass;

// =========================================================
// FILE: CheckPlatformMaintenancePassRequest.cs
//
// PURPOSE:
// Checks a pass link Vixly IT opens to get through a running window.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CheckPlatformMaintenancePassRequest
{
    public string Token { get; init; } = string.Empty;

    public string Site { get; init; } = string.Empty;
}
