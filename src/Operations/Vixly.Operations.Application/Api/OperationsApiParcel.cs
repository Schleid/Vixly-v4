namespace Vixly.Operations.Application.Api;

// =========================================================
// FILE: OperationsApiParcel.cs
//
// PURPOSE:
// A parcel as the company API shows it (Slice 10).
//
// RESPONSIBILITIES:
// - Carry what the company API and its webhooks show
//
// IMPORTANT NOTES:
// - Ids, codes and figures only: never a password, a protected value or staff notes
// - A field is only ever added, never renamed or removed (receivers depend on it)
// =========================================================
public sealed record OperationsApiParcel(
    Guid Id,
    string? Number,
    Guid? CustomerId,
    Guid? ShipmentId,
    Guid? WarehouseId,
    string? Carrier,
    string? CarrierTrackingNumber,
    decimal? WeightKg,
    decimal? LengthCm,
    decimal? WidthCm,
    decimal? HeightCm,
    string? DeclaredContents,
    decimal? DeclaredValue,
    string? CurrencyCode,
    string? Milestone,
    DateTimeOffset? LastEventUtc,
    DateTimeOffset CreatedUtc);
