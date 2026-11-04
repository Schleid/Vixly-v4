using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsRatingInput.cs
//
// PURPOSE:
// Everything the rating engine needs for one service (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Carry the price list, extras, insurance plan, units, destination, pieces, options and the pricing moment
//
// IMPORTANT NOTES:
// - The divisor is in cm³ per kg; null for a service without volumetric weight (a full container)
// - At: the pricing date; each price list, extra and plan uses its version in force then
// - ExtraQuantities: staff's quantity for an adjustable extra (0 removes it), by its id
// =========================================================
public sealed record OperationsRatingInput(
    OperationsRateCard Card,
    IReadOnlyList<OperationsChargeExtra> Extras,
    OperationsInsurancePlan? Insurance,
    decimal? DivisorCm3PerKg,
    OperationsWeightUnit WeightUnit,
    OperationsDimensionUnit DimensionUnit,
    string? CountryCode,
    Guid? OriginWarehouseId,
    Guid? DestinationWarehouseId,
    OperationsDeliveryModes Delivery,
    IReadOnlyList<OperationsRatingPiece> Pieces,
    bool Consolidated,
    bool Insured,
    DateTimeOffset At,
    IReadOnlyList<OperationsItemType>? ItemTypes = null,
    IReadOnlyDictionary<Guid, int>? ExtraQuantities = null);
