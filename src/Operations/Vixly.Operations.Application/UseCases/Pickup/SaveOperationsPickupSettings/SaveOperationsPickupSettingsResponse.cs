using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Pickup.SaveOperationsPickupSettings;

// =========================================================
// FILE: SaveOperationsPickupSettingsResponse.cs
//
// PURPOSE:
// Save Settings → Pickup (Slice 7 step 2, R-4, R-5, R-10).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsPickupSettingsResponse
{
    private SaveOperationsPickupSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsPickupSettingsResponse Success()
    {
        return new SaveOperationsPickupSettingsResponse { Succeeded = true };
    }

    public static SaveOperationsPickupSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsPickupSettingsResponse { Error = error };
    }
}
