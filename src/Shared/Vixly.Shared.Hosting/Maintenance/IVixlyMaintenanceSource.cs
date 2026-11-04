namespace Vixly.Shared.Hosting.Maintenance;

// =========================================================
// FILE: IVixlyMaintenanceSource.cs
//
// PURPOSE:
// Where a site reads the maintenance windows that close it.
//
// RESPONSIBILITIES:
// - Read the announced and running windows of one site
// - Check a pass link
//
// IMPORTANT NOTES:
// - Each plane implements it over its own route to Platform
// - Returns null when Platform cannot be reached: the site stays open
// =========================================================
public interface IVixlyMaintenanceSource
{
    Task<IReadOnlyList<VixlyMaintenanceWindow>?> ReadAsync(string site, CancellationToken cancellationToken);

    Task<DateTimeOffset?> CheckPassAsync(string site, string token, CancellationToken cancellationToken);
}
