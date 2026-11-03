namespace Vixly.Platform.Application.UseCases.Maintenance.ListPlatformMaintenanceWindows;

// =========================================================
// FILE: ListPlatformMaintenanceWindowsRequest.cs
//
// PURPOSE:
// Control's maintenance page: the windows to come and running, then the past ones.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformMaintenanceWindowsRequest
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
