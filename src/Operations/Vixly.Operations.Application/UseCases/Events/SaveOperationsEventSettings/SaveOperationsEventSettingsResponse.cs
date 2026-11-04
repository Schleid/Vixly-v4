using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Events.SaveOperationsEventSettings;

// =========================================================
// FILE: SaveOperationsEventSettingsResponse.cs
//
// PURPOSE:
// Save Settings → Events (Slice 6C, E-3 and E-5): the emails and the standard causes switched off.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsEventSettingsResponse
{
    private SaveOperationsEventSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsEventSettingsResponse Success()
    {
        return new SaveOperationsEventSettingsResponse { Succeeded = true };
    }

    public static SaveOperationsEventSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsEventSettingsResponse { Error = error };
    }
}
