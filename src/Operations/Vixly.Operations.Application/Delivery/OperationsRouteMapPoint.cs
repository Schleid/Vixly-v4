namespace Vixly.Operations.Application.Delivery;

// =========================================================
// FILE: OperationsRouteMapPoint.cs
//
// PURPOSE:
// One place on the route's map (Slice 12C, owner H-5).
//
// RESPONSIBILITIES:
// - Carry the place, its accuracy and when the phone took it
//
// IMPORTANT NOTES:
// - Six decimals at most (about 10 cm)
// =========================================================
public sealed record OperationsRouteMapPoint(decimal Latitude, decimal Longitude, int AccuracyMeters, DateTimeOffset TakenUtc);
