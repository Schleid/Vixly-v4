using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SecuritySettings.GetPlatformSecuritySettings;

// =========================================================
// FILE: GetPlatformSecuritySettingsResponse.cs
//
// PURPOSE:
// Control's sign-in rule, who changed it and whether the reader may change it.
//
// RESPONSIBILITIES:
// - Carry the values named above, nothing more
//
// IMPORTANT NOTES:
// - Part of Require MFA (owner, 2026-10-22)
// =========================================================
public sealed class GetPlatformSecuritySettingsResponse
{
    private GetPlatformSecuritySettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool RequireMfa { get; private init; } = true;

    public string? UpdatedBy { get; private init; }

    public DateTimeOffset? UpdatedUtc { get; private init; }

    public bool CanEdit { get; private init; }

    // The inactivity sign-out (owner, 2026-10-27): the security administrator changes it, not only a SuperUser.
    public int IdleMinutes { get; private init; } = 5;

    public int IdleWarnMinutes { get; private init; } = 1;

    public bool CanEditIdle { get; private init; }

    public static GetPlatformSecuritySettingsResponse Success(bool requireMfa, string? updatedBy, DateTimeOffset? updatedUtc, bool canEdit,
        int idleMinutes = 5, int idleWarnMinutes = 1, bool canEditIdle = false)
    {
        return new GetPlatformSecuritySettingsResponse
        {
            Succeeded = true,
            RequireMfa = requireMfa,
            UpdatedBy = updatedBy,
            UpdatedUtc = updatedUtc,
            CanEdit = canEdit,
            IdleMinutes = idleMinutes,
            IdleWarnMinutes = idleWarnMinutes,
            CanEditIdle = canEditIdle
        };
    }

    public static GetPlatformSecuritySettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformSecuritySettingsResponse { Error = error };
    }
}
