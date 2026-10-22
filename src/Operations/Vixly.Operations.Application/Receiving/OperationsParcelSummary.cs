using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsParcelSummary.cs
//
// PURPOSE:
// A parcel as lists show it (Slice 3B).
//
// RESPONSIBILITIES:
// - Number, carrier tracking number, customer, warehouse, measurements, milestone, holds
//
// IMPORTANT NOTES:
// - Measurements stay in kilograms and centimetres; pages convert them to the tenant's units
// =========================================================
public sealed record OperationsParcelSummary(
    Guid ParcelId,
    string? Number,
    string? CarrierTrackingNumber,
    string? Carrier,
    Guid? CustomerId,
    string? CustomerCode,
    string? CustomerName,
    Guid? WarehouseId,
    string? WarehouseCode,
    decimal? WeightKg,
    decimal? LengthCm,
    decimal? WidthCm,
    decimal? HeightCm,
    string? DeclaredContents,
    decimal? DeclaredValue,
    string? CurrencyCode,
    OperationsMilestone? Milestone,
    OperationsParcelFlags Flags,
    OperationsHoldReasons HoldReasons,
    DateTimeOffset ReceivedUtc,
    string? StorageLocationCode,
    DateTimeOffset? ShippingLabelUtc = null,
    string? LabelName = null,
    string? LabelCode = null,
    string? LabelSender = null);
