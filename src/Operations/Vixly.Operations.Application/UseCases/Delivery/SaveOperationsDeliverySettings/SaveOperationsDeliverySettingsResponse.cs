using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.SaveOperationsDeliverySettings;

// =========================================================
// FILE: SaveOperationsDeliverySettingsResponse.cs
//
// PURPOSE:
// Save Settings → Delivery (Slice 7 step 3, R-12 to R-15).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsDeliverySettingsResponse
{
    private SaveOperationsDeliverySettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsDeliverySettingsResponse Success()
    {
        return new SaveOperationsDeliverySettingsResponse { Succeeded = true };
    }

    public static SaveOperationsDeliverySettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsDeliverySettingsResponse { Error = error };
    }
}
