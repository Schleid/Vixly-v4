using Vixly.Contracts.Operations.Internal;
using Vixly.Shared.Hosting.Maintenance;

namespace Vixly.Customer.Web.Operations;

// =========================================================
// FILE: CustomerMaintenanceSource.cs
//
// PURPOSE:
// The maintenance windows of the customer portals, read through Operations.Api.
//
// RESPONSIBILITIES:
// - Turn the answer of Operations into the shared window
//
// IMPORTANT NOTES:
// - Operations unreachable: null, so the portal stays open
// =========================================================
public sealed class CustomerMaintenanceSource : IVixlyMaintenanceSource
{
    private readonly CustomerOperationsApi _operations;

    public CustomerMaintenanceSource(CustomerOperationsApi operations)
    {
        _operations = operations ?? throw new ArgumentNullException(nameof(operations));
    }

    public async Task<IReadOnlyList<VixlyMaintenanceWindow>?> ReadAsync(string site, CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationsMaintenanceWindowDto>? windows = await _operations.GetMaintenanceAsync(cancellationToken);
        return windows?.Select(window => new VixlyMaintenanceWindow(window.Id, window.StartsUtc, window.EndsUtc, window.MessageEn, window.MessageFr, window.MessageEs)).ToArray();
    }

    public Task<DateTimeOffset?> CheckPassAsync(string site, string token, CancellationToken cancellationToken)
    {
        return _operations.CheckMaintenancePassAsync(token, cancellationToken);
    }
}
