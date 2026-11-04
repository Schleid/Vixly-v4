using Vixly.Operations.Domain.Pickup;

namespace Vixly.Operations.Application.UseCases.Pickup.SaveOperationsPickupSettings;

// =========================================================
// FILE: SaveOperationsPickupSettingsRequest.cs
//
// PURPOSE:
// Save Settings → Pickup (Slice 7 step 2, R-4, R-5, R-10).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsPickupSettingsRequest
{
    public OperationsPickupProofs Proofs { get; init; }

    public bool DestinationCharges { get; init; }

    public bool DestinationStorage { get; init; }

    public int FreeDays { get; init; }

    public decimal? FeePerDay { get; init; }
}
