using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsExtraTerms.cs
//
// PURPOSE:
// What an extra charge is and where it applies, as staff set it (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Carry the identity and filters of an extra in one value
//
// IMPORTANT NOTES:
// - The amount is dated: OperationsExtraVersion
// =========================================================
public sealed record OperationsExtraTerms(
    string? Name,
    bool IsGovernmentFee,
    OperationsExtraTrigger Trigger,
    string? Words,
    string? ServiceCode,
    string? CountryCode,
    OperationsTransportMode? Mode = null,
    Guid? OriginWarehouseId = null,
    Guid? DestinationWarehouseId = null,
    OperationsDeliveryModes Delivery = OperationsDeliveryModes.None);
