namespace Vixly.Operations.Application.Delivery;

// =========================================================
// FILE: OperationsRouteMap.cs
//
// PURPOSE:
// What the route's map shows staff (Slice 12C, owner H-5): the driver's latest place and the way so far today.
//
// RESPONSIBILITIES:
// - Carry the positions, oldest first, and the next stop's number and address
//
// IMPORTANT NOTES:
// - Empty when the driver's phone has sent nothing yet
// =========================================================
public sealed record OperationsRouteMap(IReadOnlyList<OperationsRouteMapPoint> Points, string? NextStop, string? NextAddress);
