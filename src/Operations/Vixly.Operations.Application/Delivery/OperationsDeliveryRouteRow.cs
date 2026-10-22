namespace Vixly.Operations.Application.Delivery;

// =========================================================
// FILE: OperationsDeliveryRouteRow.cs
//
// PURPOSE:
// A delivery route in a list (Slice 7 step 3).
//
// RESPONSIBILITIES:
// - Carry the values a page shows
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsDeliveryRouteRow(Guid Id, string Number, DateOnly ServiceDate, string DriverName, Vixly.Operations.Domain.Delivery.OperationsRouteStatus Status, int Stops, int Done, decimal CashCollected);
