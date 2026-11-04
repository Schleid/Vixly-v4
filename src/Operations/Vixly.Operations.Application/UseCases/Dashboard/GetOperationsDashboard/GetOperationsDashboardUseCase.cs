using Vixly.Operations.Application.Dashboard;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dashboard.GetOperationsDashboard;

// =========================================================
// FILE: GetOperationsDashboardUseCase.cs
//
// PURPOSE:
// The staff home page’s counters (Slice 11): what waits, for each list this person may open.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Signed-in staff; each counter needs the right of its list, over the warehouses that right covers
// =========================================================
public sealed class GetOperationsDashboardUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsDashboardReader _reader;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsDashboardUseCase(
        OperationsAccess access,
        IOperationsDashboardReader reader,
        IOperationsLocationRepository locations,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsDashboardResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsDashboardRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // Any signed-in staff member: each counter then needs its own right.
        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsDashboardResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        IReadOnlyList<Vixly.Operations.Domain.Locations.OperationsWarehouse> warehouses = await _locations.ListWarehousesAsync(cancellationToken);
        Vixly.Operations.Domain.Settings.OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        DateOnly today = DateOnly.FromDateTime(TimeZoneInfo.TryFindSystemTimeZoneById(settings?.TimeZone ?? "UTC", out TimeZoneInfo? zone)
            ? TimeZoneInfo.ConvertTime(utcNow, zone).DateTime : utcNow.UtcDateTime);
        Dictionary<string, IReadOnlyCollection<Guid>?> asks = new(StringComparer.Ordinal);
        List<(string Key, string Link)> shown = new();

        foreach ((string key, string permission, string link, bool perWarehouse) in OperationsDashboardCounters.All)
        {
            if (!access.Has(permission) || (!perWarehouse && !access.HasTenantWide(permission)))
            {
                continue;
            }

            // Warehouse-scoped staff count their own warehouses only.
            asks[key] = access.HasTenantWide(permission) ? null
                : warehouses.Where(warehouse => access.CoversWarehouse(permission, warehouse.Id, warehouse.BranchId)).Select(warehouse => warehouse.Id).ToList();
            shown.Add((key, link));
        }

        // Harmonization H-4 (owner, 2026-11-04): every counter this person may see, in one database command.
        IReadOnlyDictionary<string, int> counts = asks.Count == 0 ? new Dictionary<string, int>() : await _reader.CountManyAsync(asks, today, cancellationToken);
        return GetOperationsDashboardResponse.Success(shown.Select(item => new OperationsDashboardCounter(item.Key, item.Link, counts.GetValueOrDefault(item.Key))).ToList());
    }
}
