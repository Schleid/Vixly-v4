using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.UseCases.Charges.SaveOperationsChargeExtra;

// =========================================================
// FILE: SaveOperationsChargeExtraRequest.cs
//
// PURPOSE:
// Adds an extra charge or changes one; a new amount can be scheduled from a date (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsChargeExtraRequest
{
    public Guid? ExtraId { get; init; }

    public string? Name { get; init; }

    public bool IsGovernmentFee { get; init; }

    public decimal? Amount { get; init; }

    public OperationsExtraCalculation Calculation { get; init; } = OperationsExtraCalculation.Fixed;

    public OperationsExtraPer Per { get; init; } = OperationsExtraPer.Item;

    public OperationsExtraTrigger Trigger { get; init; } = OperationsExtraTrigger.Always;

    public string? Words { get; init; }

    public string? ServiceCode { get; init; }

    public string? CountryCode { get; init; }

    // Slice 4C: the mode (null: all), and the route (null: any origin, any destination).
    public OperationsTransportMode? Mode { get; init; }

    public Guid? OriginWarehouseId { get; init; }

    public Guid? DestinationWarehouseId { get; init; }

    public OperationsDeliveryModes Delivery { get; init; } = OperationsDeliveryModes.None;

    public DateOnly? EffectiveFrom { get; init; }

    public bool Active { get; init; } = true;
}
