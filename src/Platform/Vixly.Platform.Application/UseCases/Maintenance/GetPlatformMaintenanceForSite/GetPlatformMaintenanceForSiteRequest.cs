namespace Vixly.Platform.Application.UseCases.Maintenance.GetPlatformMaintenanceForSite;

// =========================================================
// FILE: GetPlatformMaintenanceForSiteRequest.cs
//
// PURPOSE:
// What one site must show: windows announced or running that close it.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformMaintenanceForSiteRequest
{
    public string Site { get; init; } = string.Empty;
}
