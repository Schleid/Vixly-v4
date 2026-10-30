namespace Vixly.Operations.Application.Delivery;

// =========================================================
// FILE: OperationsDeliveryDriver.cs
//
// PURPOSE:
// A person who may drive a route from a warehouse (Slice 7 step 3, R-12).
//
// RESPONSIBILITIES:
// - Carry the values a page shows
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsDeliveryDriver(Guid UserId, string Name);
