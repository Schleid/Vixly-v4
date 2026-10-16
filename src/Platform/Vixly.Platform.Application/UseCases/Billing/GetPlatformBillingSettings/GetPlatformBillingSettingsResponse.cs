using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.GetPlatformBillingSettings;

// =========================================================
// FILE: GetPlatformBillingSettingsResponse.cs
//
// PURPOSE:
// Reads billing settings and the company profile (D-B4, D-B12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformBillingSettingsResponse
{
    private GetPlatformBillingSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformBillingSettingsItem? Settings { get; private init; }

    public bool CanEdit { get; private init; }

    public static GetPlatformBillingSettingsResponse Success(PlatformBillingSettingsItem? settings, bool canEdit)
    {
        return new GetPlatformBillingSettingsResponse { Succeeded = true, Settings = settings, CanEdit = canEdit };
    }

    public static GetPlatformBillingSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformBillingSettingsResponse { Error = error };
    }
}
