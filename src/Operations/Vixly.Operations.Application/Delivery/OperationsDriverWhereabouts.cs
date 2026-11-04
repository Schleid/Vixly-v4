namespace Vixly.Operations.Application.Delivery;

// =========================================================
// FILE: OperationsDriverWhereabouts.cs
//
// PURPOSE:
// What a customer is told about the driver bringing their shipment (Slice 12C, owner H-5).
//
// RESPONSIBILITIES:
// - The stops still before theirs; the driver's place only once theirs is the next stop
//
// IMPORTANT NOTES:
// - Out of date positions (over 15 minutes) are not shown
// =========================================================
public sealed record OperationsDriverWhereabouts(string ShipmentNumber, bool Delivered, int StopsAway, decimal? Latitude, decimal? Longitude, DateTimeOffset? TakenUtc);
