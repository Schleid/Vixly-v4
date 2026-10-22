namespace Vixly.Operations.Application.UseCases.Events.SaveOperationsEventSettings;

// =========================================================
// FILE: SaveOperationsEventSettingsRequest.cs
//
// PURPOSE:
// Save Settings → Events (Slice 6C, E-3 and E-5): the emails and the standard causes switched off.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsEventSettingsRequest
{
    public bool EventEmails { get; init; }

    // Slice 7: the ready-for-pickup email.
    public bool PickupEmails { get; init; } = true;

    public IReadOnlyList<string> DisabledCauses { get; init; } = Array.Empty<string>();
}
