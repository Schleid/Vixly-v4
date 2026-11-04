using System.Security.Cryptography;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Delivery;

namespace Vixly.Operations.Application.Delivery;

// =========================================================
// FILE: OperationsDriverTracking.cs
//
// PURPOSE:
// The rules of live location (Slice 12C, owner H-5): who sees the driver, when, and how much.
//
// RESPONSIBILITIES:
// - Tell a customer, for their stop, the stops before it and, when it is next, the driver's place
// - Make the private link of an "out for delivery" notice
//
// IMPORTANT NOTES:
// - Only while the route is out and only on the route's day (company time)
// - The link's token is random; only its hash is kept on the stop
// =========================================================
public sealed class OperationsDriverTracking
{
    public static readonly TimeSpan Fresh = TimeSpan.FromMinutes(15);

    private readonly IOperationsDriverTrackingRepository _tracking;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public OperationsDriverTracking(IOperationsDriverTrackingRepository tracking, IOperationsSettingsRepository settings, TimeProvider timeProvider)
    {
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public static string NewToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public async Task<OperationsDriverWhereabouts?> WhereAsync(OperationsDeliveryStop stop, OperationsDeliveryRoute route, string shipmentNumber, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stop);
        ArgumentNullException.ThrowIfNull(route);
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        string? timeZone = (await _settings.FindAsync(cancellationToken))?.TimeZone;

        if (route.ServiceDate != Vixly.Operations.Domain.Locations.OperationsTimeZones.Today(utcNow, timeZone))
        {
            return null;
        }

        if (stop.Status == OperationsStopStatus.Delivered)
        {
            return new OperationsDriverWhereabouts(shipmentNumber, true, 0, null, null, null);
        }

        if (route.Status != OperationsRouteStatus.Out || stop.IsDone)
        {
            return null;
        }

        int before = await _tracking.StopsBeforeAsync(route.Id, stop.Sequence, cancellationToken);
        OperationsDriverPosition? position = before == 0 ? await _tracking.LatestAsync(route.Id, cancellationToken) : null;

        return position is not null && utcNow - position.TakenUtc <= Fresh
            ? new OperationsDriverWhereabouts(shipmentNumber, false, 0, position.Latitude, position.Longitude, position.TakenUtc)
            : new OperationsDriverWhereabouts(shipmentNumber, false, before, null, null, null);
    }
}
