using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.UpdatePlatformTenantSettings;

// =========================================================
// FILE: UpdatePlatformTenantSettingsResponse.cs
//
// PURPOSE:
// Result of UpdatePlatformTenantSettings.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformTenantSettingsResponse
{
    private UpdatePlatformTenantSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformTenantSettingsResponse Success()
    {
        return new UpdatePlatformTenantSettingsResponse { Succeeded = true };
    }

    public static UpdatePlatformTenantSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformTenantSettingsResponse { Error = error };
    }
}
