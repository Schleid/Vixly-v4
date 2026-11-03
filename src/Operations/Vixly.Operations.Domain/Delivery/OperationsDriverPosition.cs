using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Delivery;

// =========================================================
// FILE: OperationsDriverPosition.cs
//
// PURPOSE:
// Where the driver of a running route was, as their phone said (Slice 12C, owner H-5).
//
// RESPONSIBILITIES:
// - Keep one position: the route, the driver, the place, its accuracy and the time
//
// IMPORTANT NOTES:
// - Only while the route is out, only from its driver's page (the use case checks)
// - Kept 7 days after it was taken, then deleted by the housekeeping
// - Bound to its tenant by OperationsEntity
// =========================================================
public sealed class OperationsDriverPosition : OperationsEntity
{
    public static readonly TimeSpan KeptFor = TimeSpan.FromDays(7);

    private OperationsDriverPosition()
    {
    }

    private OperationsDriverPosition(Guid tenantId, Guid routeId, Guid userId, decimal latitude, decimal longitude, int accuracyMeters, DateTimeOffset takenUtc, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        RouteId = routeId;
        UserId = userId;
        Latitude = latitude;
        Longitude = longitude;
        AccuracyMeters = accuracyMeters;
        TakenUtc = takenUtc;
    }

    public Guid RouteId { get; private set; }

    public Guid UserId { get; private set; }

    public decimal Latitude { get; private set; }

    public decimal Longitude { get; private set; }

    public int AccuracyMeters { get; private set; }

    public DateTimeOffset TakenUtc { get; private set; }

    // A place on Earth, to six decimals (about 10 cm); the accuracy in metres, up to 100 km.
    public static OperationsDriverPosition Take(Guid tenantId, Guid routeId, Guid userId, decimal latitude, decimal longitude, int accuracyMeters, DateTimeOffset takenUtc,
        DateTimeOffset utcNow)
    {
        if (routeId == Guid.Empty || userId == Guid.Empty || latitude is < -90m or > 90m || longitude is < -180m or > 180m || accuracyMeters is < 0 or > 100_000)
        {
            throw new Vixly.Shared.Kernel.Domain.DomainException(OperationsDeliveryErrors.PositionInvalid);
        }

        return new OperationsDriverPosition(tenantId, routeId, userId, decimal.Round(latitude, 6), decimal.Round(longitude, 6), accuracyMeters,
            takenUtc > utcNow ? utcNow : takenUtc, utcNow);
    }
}
