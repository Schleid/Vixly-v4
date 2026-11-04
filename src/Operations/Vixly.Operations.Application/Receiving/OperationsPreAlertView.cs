using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsPreAlertView.cs
//
// PURPOSE:
// A pre-alert as its customer sees it (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the customer's own declaration and whether it was matched
//
// IMPORTANT NOTES:
// - Only ever built for the pre-alert's own customer
// =========================================================
public sealed record OperationsPreAlertView(
    Guid PreAlertId,
    string Carrier,
    string TrackingNumber,
    string? Store,
    string Contents,
    decimal DeclaredValue,
    string CurrencyCode,
    DateOnly? ExpectedDate,
    OperationsPreAlertStatus Status,
    Guid? ParcelId,
    DateTimeOffset CreatedUtc,
    Guid? ShipmentId = null,
    Guid? WarehouseId = null)
{
    public static OperationsPreAlertView Of(OperationsPreAlert preAlert)
    {
        ArgumentNullException.ThrowIfNull(preAlert);

        return new(preAlert.Id, preAlert.Carrier, preAlert.TrackingNumber, preAlert.Store, preAlert.Contents, preAlert.DeclaredValue, preAlert.CurrencyCode, preAlert.ExpectedDate,
            preAlert.Status, preAlert.ParcelId, preAlert.CreatedUtc, preAlert.ShipmentId, preAlert.WarehouseId);
    }
}
