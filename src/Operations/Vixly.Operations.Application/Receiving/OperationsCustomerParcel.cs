using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsCustomerParcel.cs
//
// PURPOSE:
// A parcel as its customer sees it (Slice 3B, design section 7).
//
// RESPONSIBILITIES:
// - Carry what the customer may know about their own parcel
//
// IMPORTANT NOTES:
// - No shelf, staff member or internal note (ADR-0021 decision 5)
// =========================================================
public sealed record OperationsCustomerParcel(
    Guid ParcelId,
    string? Number,
    string? Carrier,
    string? CarrierTrackingNumber,
    string? DeclaredContents,
    decimal? DeclaredValue,
    string? CurrencyCode,
    decimal? WeightKg,
    decimal? LengthCm,
    decimal? WidthCm,
    decimal? HeightCm,
    OperationsMilestone? Milestone,
    OperationsParcelFlags Flags,
    OperationsHoldReasons HoldReasons,
    string? WarehouseName,
    DateTimeOffset ReceivedUtc,
    Guid? ShipmentId = null)
{
    public static OperationsCustomerParcel Of(OperationsParcel parcel, string? warehouseName)
    {
        ArgumentNullException.ThrowIfNull(parcel);

        return new(parcel.Id, parcel.Number, parcel.Carrier, parcel.CarrierTrackingNumber, parcel.DeclaredContents, parcel.DeclaredValue, parcel.CurrencyCode,
            parcel.WeightKg, parcel.LengthCm, parcel.WidthCm, parcel.HeightCm, parcel.Milestone, parcel.Flags, parcel.HoldReasons, warehouseName, parcel.CreatedUtc, parcel.ShipmentId);
    }
}
