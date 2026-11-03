using Vixly.Contracts.Platform.Maintenance;
using Vixly.Operations.Infrastructure.Platform;
using Vixly.Shared.Hosting.Maintenance;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsMaintenanceSource.cs
//
// PURPOSE:
// The maintenance windows of the company websites and staff workspaces, read from Platform.
//
// RESPONSIBILITIES:
// - Turn the answer of Platform into the shared window
//
// IMPORTANT NOTES:
// - Platform unreachable: null, so the sites stay open
// =========================================================
public sealed class OperationsMaintenanceSource : IVixlyMaintenanceSource
{
    private readonly OperationsMaintenanceReader _reader;

    public OperationsMaintenanceSource(OperationsMaintenanceReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<IReadOnlyList<VixlyMaintenanceWindow>?> ReadAsync(string site, CancellationToken cancellationToken)
    {
        IReadOnlyList<PlatformMaintenanceWindowInfo>? windows = await _reader.ReadAsync(site, cancellationToken);
        return windows?.Select(window => new VixlyMaintenanceWindow(window.Id, window.StartsUtc, window.EndsUtc, window.MessageEn, window.MessageFr, window.MessageEs)).ToArray();
    }

    public Task<DateTimeOffset?> CheckPassAsync(string site, string token, CancellationToken cancellationToken)
    {
        return _reader.CheckPassAsync(site, token, cancellationToken);
    }
}
