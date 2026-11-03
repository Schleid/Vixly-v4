using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SecuritySettings.UpdatePlatformSecuritySettings;

// =========================================================
// FILE: UpdatePlatformSecuritySettingsResponse.cs
//
// PURPOSE:
// Result of UpdatePlatformSecuritySettings.
//
// RESPONSIBILITIES:
// - Carry the values named above, nothing more
//
// IMPORTANT NOTES:
// - Part of Require MFA (owner, 2026-10-22)
// =========================================================
public sealed class UpdatePlatformSecuritySettingsResponse
{
    private UpdatePlatformSecuritySettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformSecuritySettingsResponse Success()
    {
        return new UpdatePlatformSecuritySettingsResponse { Succeeded = true };
    }

    public static UpdatePlatformSecuritySettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformSecuritySettingsResponse { Error = error };
    }
}
