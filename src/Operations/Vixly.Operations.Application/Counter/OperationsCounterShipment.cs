using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.Counter;

// =========================================================
// FILE: OperationsCounterShipment.cs
//
// PURPOSE:
// One of the customer's shipments on the counter (Slice 5C, C-5).
//
// RESPONSIBILITIES:
// - Carry where it goes, for whom, its parcels against the count, its status and its amount once final
//
// IMPORTANT NOTES:
// - CanServe: it leaves from a warehouse the clerk serves at, so parcels can be added there
// - CreatedAtCounter: its count is the clerk's to correct until the first parcel is in (C-1)
// =========================================================
public sealed record OperationsCounterShipment(
    Guid Id,
    string? Number,
    DateTimeOffset CreatedUtc,
    string? Origin,
    string? Destination,
    bool HomeDelivery,
    string? ServiceCode,
    string? RecipientName,
    int? ParcelCount,
    int Entered,
    OperationsShipmentStatus Status,
    bool HasInstructions,
    decimal? BookedTotal,
    string? CurrencyCode,
    bool CanServe,
    bool CreatedAtCounter,
    bool Insured = false,
    Guid? InsurancePlanId = null,
    DateTimeOffset? CustomerSignedUtc = null);
