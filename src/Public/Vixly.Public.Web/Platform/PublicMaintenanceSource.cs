using Vixly.Contracts.Platform.Maintenance;
using Vixly.Shared.Hosting.Maintenance;

namespace Vixly.Public.Web.Platform;

// =========================================================
// FILE: PublicMaintenanceSource.cs
//
// PURPOSE:
// The maintenance windows of the public site, read from Platform.
//
// RESPONSIBILITIES:
// - Turn the answer of Platform into the shared window
//
// IMPORTANT NOTES:
// - Platform unreachable: null, so the site stays open
// =========================================================
public sealed class PublicMaintenanceSource : IVixlyMaintenanceSource
{
    private readonly PlatformApi _platform;

    public PublicMaintenanceSource(PlatformApi platform)
    {
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
    }

    public async Task<IReadOnlyList<VixlyMaintenanceWindow>?> ReadAsync(string site, CancellationToken cancellationToken)
    {
        PlatformApiResult<IReadOnlyList<PlatformMaintenanceWindowInfo>> result = await _platform.MaintenanceAsync(cancellationToken);
        return result.Succeeded && result.Value is { } windows
            ? windows.Select(window => new VixlyMaintenanceWindow(window.Id, window.StartsUtc, window.EndsUtc, window.MessageEn, window.MessageFr, window.MessageEs)).ToArray()
            : null;
    }

    public async Task<DateTimeOffset?> CheckPassAsync(string site, string token, CancellationToken cancellationToken)
    {
        PlatformApiResult<PlatformMaintenancePassInfo> result = await _platform.MaintenancePassAsync(token, cancellationToken);
        return result.Succeeded && result.Value is { } pass ? pass.EndsUtc : null;
    }
}
