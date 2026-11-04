namespace Vixly.Operations.Application.UseCases.Delivery.RecordOperationsDriverPosition;

// =========================================================
// FILE: RecordOperationsDriverPositionRequest.cs
//
// PURPOSE:
// Keeps the position the driver's phone sends while the route is out (Slice 12C, owner H-5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RecordOperationsDriverPositionRequest
{
    public Guid RouteId { get; init; }

    public decimal Latitude { get; init; }

    public decimal Longitude { get; init; }

    public int AccuracyMeters { get; init; }

    public DateTimeOffset? TakenUtc { get; init; }
}
