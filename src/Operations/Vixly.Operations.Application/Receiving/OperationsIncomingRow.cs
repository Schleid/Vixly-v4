using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsIncomingRow.cs
//
// PURPOSE:
// One pre-alert in the incoming packages list (Slice 4D, A-7).
//
// RESPONSIBILITIES:
// - The customer, the package as announced, its parcel when received, and whether it is in a shipment
//
// IMPORTANT NOTES:
// - Read only
// =========================================================
public sealed record OperationsIncomingRow(
    Guid PreAlertId,
    Guid? WarehouseId,
    string? CustomerCode,
    string CustomerName,
    string Carrier,
    string TrackingNumber,
    string? Store,
    string Contents,
    decimal DeclaredValue,
    string CurrencyCode,
    DateOnly? ExpectedDate,
    OperationsPreAlertStatus Status,
    Guid? ParcelId,
    string? ParcelNumber,
    bool InShipment,
    DateTimeOffset CreatedUtc);
