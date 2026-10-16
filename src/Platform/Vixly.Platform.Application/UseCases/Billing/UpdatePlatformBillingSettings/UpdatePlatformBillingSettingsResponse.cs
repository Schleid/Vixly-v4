using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.UpdatePlatformBillingSettings;

// =========================================================
// FILE: UpdatePlatformBillingSettingsResponse.cs
//
// PURPOSE:
// Changes payment terms, number prefixes, paper size, the email switch and the invoice texts (D-B4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformBillingSettingsResponse
{
    private UpdatePlatformBillingSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformBillingSettingsResponse Success()
    {
        return new UpdatePlatformBillingSettingsResponse { Succeeded = true };
    }

    public static UpdatePlatformBillingSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformBillingSettingsResponse { Error = error };
    }
}
