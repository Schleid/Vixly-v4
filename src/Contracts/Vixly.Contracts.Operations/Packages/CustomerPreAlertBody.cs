namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerPreAlertBody.cs
//
// PURPOSE:
// A pre-alert the customer writes on the portal (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerPreAlertBody(string? Carrier, string? TrackingNumber, string? Store, string? Contents, decimal DeclaredValue, DateOnly? ExpectedDate, Guid? ShipmentId = null,
    Guid? WarehouseId = null);
