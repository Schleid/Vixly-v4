namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerDriverDto.cs
//
// PURPOSE:
// Where the driver bringing a customer's shipment is (Slice 12C, owner H-5).
//
// RESPONSIBILITIES:
// - Carry the shipment's number, whether it was delivered, the stops before it and, when it is next, the driver's place
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// - MapAttribution is the map's credit line; none when no map is configured (the portal then shows words only)
// =========================================================
public sealed record CustomerDriverDto(string ShipmentNumber, bool Delivered, int StopsAway, decimal? Latitude, decimal? Longitude, DateTimeOffset? TakenUtc, string? MapAttribution);
