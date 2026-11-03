namespace Vixly.Operations.Application.Api;

// =========================================================
// FILE: OperationsApiShipment.cs
//
// PURPOSE:
// A shipment as the company API shows it (Slice 10).
//
// RESPONSIBILITIES:
// - Carry what the company API and its webhooks show
//
// IMPORTANT NOTES:
// - Ids, codes and figures only: never a password, a protected value or staff notes
// - A field is only ever added, never renamed or removed (receivers depend on it)
// =========================================================
public sealed record OperationsApiShipment(
    Guid Id,
    string? Number,
    Guid CustomerId,
    string Status,
    Guid OriginWarehouseId,
    Guid? DestinationWarehouseId,
    Guid? DestinationBranchId,
    string? DestinationCountryCode,
    string? ServiceCode,
    string? RecipientName,
    int? ParcelCount,
    decimal? BookedTotal,
    string? CurrencyCode,
    DateTimeOffset? BookedUtc,
    DateTimeOffset? PickupReadyUtc,
    DateTimeOffset? PickedUpUtc,
    DateTimeOffset? DeliveredUtc,
    DateTimeOffset CreatedUtc);
