using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsEstimateInput.cs
//
// PURPOSE:
// What a shipment estimate is computed from: the shipment's choices and its parcels so far (Slice 5D).
//
// RESPONSIBILITIES:
// - Carry the customer, route, service, delivery, options, insurance product and the parcels with their labels
//
// IMPORTANT NOTES:
// - ExpectedValue: the declared value the customer expects while no parcel is in (the portal, the new shipment form)
// =========================================================
public sealed record OperationsEstimateInput(
    Guid? CustomerId,
    string? ServiceCode,
    Guid? OriginWarehouseId,
    Guid? DestinationWarehouseId,
    OperationsDeliveryModes Delivery,
    bool Consolidate,
    Guid? InsurancePlanId,
    IReadOnlyList<(string? Label, OperationsRatingPiece Piece)> Parcels,
    decimal? ExpectedValue = null,
    IReadOnlyList<OperationsEstimateSupply>? Supplies = null);
