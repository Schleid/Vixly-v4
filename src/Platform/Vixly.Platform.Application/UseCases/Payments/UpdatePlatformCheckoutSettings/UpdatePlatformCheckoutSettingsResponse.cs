using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.UpdatePlatformCheckoutSettings;

// =========================================================
// FILE: UpdatePlatformCheckoutSettingsResponse.cs
//
// PURPOSE:
// Changes the checkout page look and the bank statement wording.
//
// RESPONSIBILITIES:
// - Report success, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformCheckoutSettingsResponse
{
    private UpdatePlatformCheckoutSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformCheckoutSettingsResponse Success()
    {
        return new UpdatePlatformCheckoutSettingsResponse { Succeeded = true };
    }

    public static UpdatePlatformCheckoutSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformCheckoutSettingsResponse { Error = error };
    }
}
