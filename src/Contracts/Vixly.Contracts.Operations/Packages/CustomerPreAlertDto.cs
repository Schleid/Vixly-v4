namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerPreAlertDto.cs
//
// PURPOSE:
// An open pre-alert of the customer (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerPreAlertDto(Guid PreAlertId, string Carrier, string TrackingNumber, string? Store, string Contents, decimal DeclaredValue, string CurrencyCode, DateOnly? ExpectedDate, DateTimeOffset CreatedUtc, Guid? ShipmentId = null,
    Guid? WarehouseId = null,
    string? Warehouse = null);
